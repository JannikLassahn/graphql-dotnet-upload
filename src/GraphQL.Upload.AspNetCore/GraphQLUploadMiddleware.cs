using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Upload.AspNetCore
{
    class GraphQLRequest
    {
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";
        public const string OperationNameKey = "operationName";
        public const string MapKey = "map";

        [JsonProperty(QueryKey)]
        public string Query { get; set; }

        [JsonProperty(VariablesKey)]
        public JObject Variables { get; set; }

        [JsonProperty(OperationNameKey)]
        public string OperationName { get; set; }

        [JsonIgnore]
        public List<Meta> TokensToReplace { get; set; }

        public Inputs GetInputs()
        {
            var variables = Variables?.ToInputs();

            // the following implementation seems brittle because of a lot of casting
            // and it depends on the types that ToInputs() creates.

            foreach (var info in TokensToReplace)
            {
                int i = 0;
                object o = variables;

                foreach (var p in info.Parts)
                {
                    var isLast = i++ == info.Parts.Count - 1;

                    if (p is string s)
                    {
                        if (isLast)
                            ((Inputs)o)[s] = info.File;
                        else
                            o = ((Inputs)o)[s];
                    }
                    else if (p is int index)
                    {
                        if (isLast)
                            ((List<object>)o)[index] = info.File;
                        else
                            o = ((List<object>)o)[index];
                    }
                }
            }

            return variables;
        }
    }

    class Meta
    {
        public List<object> Parts { get; set; }

        public IFormFile File { get; set; }
    }

    public class GraphQLMultipartMiddleware<TSchema>
           where TSchema : ISchema
    {
        private const string SpecUrl = "https://github.com/jaydenseric/graphql-multipart-request-spec";

        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly PathString _path;

        public GraphQLMultipartMiddleware(ILogger<GraphQLMultipartMiddleware<TSchema>> logger, RequestDelegate next, PathString path)
        {
            _logger = logger;
            _next = next;
            _path = path;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsMultipartGraphQLRequest(context.Request))
            {
                await _next(context);
                return;
            }

            var httpRequest = context.Request;
            var formCollection = await httpRequest.ReadFormAsync();
            var requests = ExtractGraphQLRequestsFromPostBody(formCollection);

            var executer = context.RequestServices.GetRequiredService<IDocumentExecuter>();
            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();
            var schema = context.RequestServices.GetRequiredService<ISchema>();

            if (requests is null)
            {
                await WriteBadRequestResponseAsync(context, writer, "Invalid GraphQL request");
                return;
            }

            var results = await Task.WhenAll(
                requests.Select(request => executer.ExecuteAsync(new ExecutionOptions
                {
                    CancellationToken = context.RequestAborted,
                    Schema = schema,
                    Query = request.Query,
                    OperationName = request.OperationName,
                    Inputs = request.GetInputs()
                }
            )));


            foreach (var result in results)
            {
                if (result.Errors is null)
                    continue;

                _logger.LogError("GraphQL execution error(s): {Errors}", result.Errors);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;

            if (results.Length == 1)
                await WriteResponseAsync(context, writer, results[0]);
            else
                await WriteResponsesAsync(context, writer, results);
        }


        private bool IsMultipartGraphQLRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments(_path) &&
                    request.HasFormContentType;
        }

        private Task WriteBadRequestResponseAsync(HttpContext context, IDocumentWriter writer, string errorMessage)
        {
            var result = new ExecutionResult()
            {
                Errors = new ExecutionErrors()
                {
                    new ExecutionError(errorMessage)
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 400; // Bad Request

            return writer.WriteAsync(context.Response.Body, result);
        }

        private async Task WriteResponsesAsync(HttpContext context, IDocumentWriter writer, ExecutionResult[] results)
        {
            using (var sw = new StreamWriter(context.Response.Body, Encoding.UTF8))
            {
                sw.AutoFlush = true;
                sw.Write("[");
                for (int i = 0; i <= results.Length - 2; i++)
                {
                    await writer.WriteAsync(context.Response.Body, results[i]);
                    sw.Write(",");
                }
                await writer.WriteAsync(context.Response.Body, results[results.Length - 1]);
                sw.Write("]");
            }
        }

        private Task WriteResponseAsync(HttpContext context, IDocumentWriter writer, ExecutionResult result)
        {
            return writer.WriteAsync(context.Response.Body, result);
        }

        private static List<GraphQLRequest> ExtractGraphQLRequestsFromPostBody(IFormCollection fc)
        {
            List<GraphQLRequest> requests;

            fc.TryGetValue("operations", out var operationsJson);
            var operations = JToken.Parse(operationsJson);

            var map = fc.TryGetValue(GraphQLRequest.MapKey, out var mapValues) ? JsonConvert.DeserializeObject<Dictionary<string, string[]>>(mapValues[0]) : null;
            var mapMap = new Dictionary<int, List<Meta>>();

            foreach ((var fileName, var paths) in map)
            {
                var file = fc.Files.GetFile(fileName);

                foreach (var path in paths)
                {
                    (var index, var parts) = GetParts(path, operations is JArray);

                    if (!mapMap.ContainsKey(index))
                    {
                        mapMap.Add(index, new List<Meta>());
                    }

                    mapMap[index].Add(new Meta { File = file, Parts = parts });
                }
            }

            if (operations is JArray)
            {
                int i = 0;
                requests = operations.Select(j =>
                {
                    return CreateGraphQLRequest(j, mapMap, i++);
                }).ToList();
            }
            else
            {
                var request = CreateGraphQLRequest(operations, mapMap, 0);
                requests = new List<GraphQLRequest> { request };
            }


            return requests;
        }

        private static GraphQLRequest CreateGraphQLRequest(JToken j, Dictionary<int, List<Meta>> mapMap, int index)
        {
            var request = j.ToObject<GraphQLRequest>();
            if (mapMap.ContainsKey(index))
                request.TokensToReplace = mapMap[index];
            return request;
        }

        private static (int requestIndex, List<object> parts) GetParts(string path, bool isBatchedRequest)
        {
            var results = new List<object>();
            var requestIndex = 0;

            foreach (var s in path.Split('.'))
            {
                if (int.TryParse(s, out int integer))
                    results.Add(integer);
                else
                    results.Add(s);
            }

            if (isBatchedRequest)
            {
                requestIndex = (int)results[0];
                results.RemoveRange(0, 2);
            }
            else
            {
                results.RemoveAt(0);
            }

            return (requestIndex, results);
        }
    }
}
