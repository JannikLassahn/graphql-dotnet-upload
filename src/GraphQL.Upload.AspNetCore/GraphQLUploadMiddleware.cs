using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadMiddleware<TSchema>
           where TSchema : ISchema
    {
        private const string SpecUrl = "https://github.com/jaydenseric/graphql-multipart-request-spec";

        private readonly RequestDelegate _next;
        private readonly GraphQLUploadOptions _options;

        public GraphQLUploadMiddleware(RequestDelegate next, GraphQLUploadOptions options)
        {
            _next = next ?? throw new ArgumentException(nameof(next));
            _options = options ?? throw new ArgumentException(nameof(options));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsMultipartGraphQLRequest(context))
            {
                await _next.Invoke(context);
                return;
            }

            var forms = await context.Request.ReadFormAsync();

            // validate file count
            if (_options.MaximumFileCount.HasValue &&
                _options.MaximumFileCount < forms.Files.Count)
            {
                await WriteStatusCodeWithMessage(context, 413, $"{_options.MaximumFileCount} file uploads exceeded.");
                return;
            }

            // validate file size
            if(_options.MaximumFileSize.HasValue)
            {
                foreach (var file in forms.Files)
                {
                    if(file.Length > _options.MaximumFileSize)
                    {
                        await WriteStatusCodeWithMessage(context, 413, "File size limit exceeded.");
                        return;
                    }
                }
            }

            if(!forms.TryGetValue("operations", out var operationsJson))
            {
                await WriteStatusCodeWithMessage(context, 400, $"Missing field 'operations' ({SpecUrl}).");
                return;
            }

            if(!forms.TryGetValue("map", out var mapJson))
            {
                await WriteStatusCodeWithMessage(context, 400, $"Missing field 'map' ({SpecUrl}).");
                return;
            }

            (var requests, var error) = ExtractGraphQLRequests(operationsJson, mapJson, forms);
            if(error != null)
            {
                await WriteStatusCodeWithMessage(context, 400, error);
                return;
            }

            var executer = context.RequestServices.GetRequiredService<IDocumentExecuter>();
            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();
            var schema = context.RequestServices.GetRequiredService<ISchema>();

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

            await WriteResponsesAsync(context, writer, results);
        }


        private static bool IsMultipartGraphQLRequest(HttpContext context)
        {
            return context.Request.HasFormContentType;
        }

        private static Task WriteStatusCodeWithMessage(HttpContext context, int code, string message)
        {
            context.Response.StatusCode = code;
            return context.Response.WriteAsync(message);
        }

        private async Task WriteResponsesAsync(HttpContext context, IDocumentWriter writer, ExecutionResult[] results)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;

            if(results.Length == 1)
            {
                await writer.WriteAsync(context.Response.Body, results[0]);
                return;
            }

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

        private static (List<GraphQLRequest> requests, string error) ExtractGraphQLRequests(string operationsJson, string mapJson, IFormCollection forms)
        {
            Dictionary<string, string[]> map;
            JToken operations;

            try
            {
                operations = JToken.Parse(operationsJson);
            }
            catch (JsonException)
            {
                return (null, $"Invalid JSON in the 'operations' multipart field ({SpecUrl}).");
            }

            try
            {
                map = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(mapJson);
            }
            catch (JsonException)
            {
                return (null, $"Invalid JSON in the 'map' multipart field ({SpecUrl}).");
            }

            List<GraphQLRequest> requests;
            var metaLookup = new Dictionary<int, List<Meta>>();

            foreach (var entry in map)
            {
                var file = forms.Files.GetFile(entry.Key);
                if(file is null)
                {
                    return (null, "File is null");
                }

                foreach (var path in entry.Value)
                {
                    (var index, var parts) = GetParts(path, operations is JArray);

                    if (!metaLookup.ContainsKey(index))
                    {
                        metaLookup.Add(index, new List<Meta>());
                    }

                    metaLookup[index].Add(new Meta { File = file, Parts = parts });
                }
            }

            if (operations is JArray)
            {
                int i = 0;
                requests = operations
                            .Select(j => CreateGraphQLRequest(j, metaLookup, i++))
                            .ToList();
            }
            else
            {
                var request = CreateGraphQLRequest(operations, metaLookup, 0);
                requests = new List<GraphQLRequest> { request };
            }

            return (requests, null);

        }

        private static GraphQLRequest CreateGraphQLRequest(JToken j, Dictionary<int, List<Meta>> metaLookup, int index)
        {
            var request = j.ToObject<GraphQLRequest>();
            if (metaLookup.ContainsKey(index))
            {
                request.TokensToReplace = metaLookup[index];
            }
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

            // remove request index and 'variables' part, 
            // because the parts list only needs to hold the parts relevant for each request
            // e.g: 0.variables.file.0 -> ["file", 0]

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

    class Meta
    {
        public List<object> Parts { get; set; }

        public IFormFile File { get; set; }
    }
}
