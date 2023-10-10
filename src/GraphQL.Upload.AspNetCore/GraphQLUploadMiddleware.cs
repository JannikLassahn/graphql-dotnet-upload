using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadMiddleware<TSchema>
        where TSchema : ISchema
    {
        private const string DOCS_URL = "See: https://github.com/jaydenseric/graphql-multipart-request-spec.";
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly GraphQLUploadOptions _options;
        private readonly GraphQLUploadRequestDeserializer _requestDeserializer;
        private readonly string _graphQLPath;
        public GraphQLUploadMiddleware(ILogger<GraphQLUploadMiddleware<TSchema>> logger, RequestDelegate next,
            GraphQLUploadOptions options, PathString path, GraphQLUploadRequestDeserializer requestDeserializer)
        {
            _logger = logger;
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options;
            _requestDeserializer = requestDeserializer;
            _graphQLPath = path;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.HasFormContentType
                || !context.Request.Path.StartsWithSegments(_graphQLPath))
            {
                // not graphql path, eg. Form Authentication, skip this middleware
                await _next(context);
                return;
            }

            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            // Inspiration: https://github.com/graphql/express-graphql/blob/master/src/index.js
            var httpRequest = context.Request;
            var httpResponse = context.Response;

            var serializer = context.RequestServices.GetRequiredService<IGraphQLTextSerializer>();
            var cancellationToken = GetCancellationToken(context);

            // GraphQL HTTP only supports GET and POST methods
            bool isPost = HttpMethods.IsPost(httpRequest.Method);
            if (!isPost)
            {
                httpResponse.Headers["Allow"] = "POST";
                await WriteErrorResponseAsync(httpResponse, serializer,
                    $"Invalid HTTP method. Only POST are supported. {DOCS_URL}",
                    httpStatusCode: 405 // Method Not Allowed
                );
                return;
            }

            var form = await context.Request.ReadFormAsync();

            int statusCode = 400;
            string error;
            GraphQLUploadRequestDeserializationResult uploadRequest;
            IFormFileCollection files;
            List<GraphQLUploadRequest> requests;

            try
            {
                uploadRequest = _requestDeserializer.DeserializeFromFormCollection(form);
            }
            catch (Exception exception)
            {
                await WriteErrorResponseAsync(httpResponse, serializer, $"{exception.Message} ${DOCS_URL}", statusCode);
                return;
            }

            (files, error, statusCode) = GetFiles(form);
            if (error != null)
            {
                await WriteErrorResponseAsync(httpResponse, serializer, error, statusCode);
                return;
            }

            (requests, error) = ExtractGraphQLRequests(uploadRequest, form);
            if (error != null)
            {
                await WriteErrorResponseAsync(httpResponse, serializer, error, statusCode);
                return;
            }

            var executer = context.RequestServices.GetRequiredService<IDocumentExecuter>();
            var schema = context.RequestServices.GetRequiredService<TSchema>();

            var results = await Task.WhenAll(
                requests.Select(request => executer.ExecuteAsync(options =>
                {
                    options.CancellationToken = context.RequestAborted;
                    options.Schema = schema;
                    options.Query = request.Query;
                    options.OperationName = request.OperationName;
                    options.Variables = request.GetVariables();
                    options.User = context.User;
                    if (_options.UserContextFactory != null)
                    {
                        options.UserContext = _options.UserContextFactory.Invoke(context);
                    }
                    options.RequestServices = context.RequestServices;
                    foreach (var listener in context.RequestServices.GetRequiredService<IEnumerable<IDocumentExecutionListener>>())
                    {
                        options.Listeners.Add(listener);
                    }
                })));

            await WriteResponseAsync(context, serializer, results);
        }

        protected virtual CancellationToken GetCancellationToken(HttpContext context) => context.RequestAborted;

        private static GraphQLUploadRequest CreateGraphQLRequest(GraphQLUploadRequest operation, Dictionary<int, List<GraphQLUploadFileMap>> metaLookup, int index)
        {
            if (metaLookup.ContainsKey(index))
            {
                operation.TokensToReplace = metaLookup[index];
            }

            return operation;
        }

        private static (int requestIndex, List<object> parts) GetParts(string path, bool isBatchedRequest)
        {
            var results = new List<object>();
            var requestIndex = 0;

            foreach (var key in path.Split('.'))
            {
                if (int.TryParse(key, out int integer))
                {
                    results.Add(integer);
                }
                else
                {
                    results.Add(key);
                }
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

        private (IFormFileCollection, string, int) GetFiles(IFormCollection form)
        {
            if (!form.Files.Any())
            {
                return (null, $"No files attached. {DOCS_URL}", 400);
            }

            // validate file count
            if (_options.MaximumFileCount < form.Files.Count)
            {
                return (null, $"{_options.MaximumFileCount} file uploads exceeded.", 413);
            }

            // validate file size
            foreach (var file in form.Files)
            {
                if (file.Length > _options.MaximumFileSize)
                {
                    return (null, "File size limit exceeded.", 413);
                }
            }

            return (form.Files, null, default);
        }

        private Task WriteErrorResponseAsync(HttpResponse httpResponse, IGraphQLTextSerializer serializer,
            string errorMessage, int httpStatusCode = 400 /* BadRequest */)
        {
            var result = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError(errorMessage)
                }
            };

            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = httpStatusCode;

            return serializer.WriteAsync(httpResponse.Body, result);
        }

        private async Task WriteResponseAsync(HttpContext context, IGraphQLTextSerializer serializer, ExecutionResult[] results)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;

            foreach (var result in results)
            {
                if (result.Errors != null)
                {
                    _logger.LogError("GraphQL execution error(s): {Errors}", result.Errors);
                }
            }

            if (results.Length == 1)
            {
                await serializer.WriteAsync(context.Response.Body, results[0]);
                return;
            }

            await serializer.WriteAsync(context.Response.Body, results);
        }

        private (List<GraphQLUploadRequest> requests, string error) ExtractGraphQLRequests(
            GraphQLUploadRequestDeserializationResult operations, IFormCollection forms)
        {
            List<GraphQLUploadRequest> requests;
            var metaLookup = new Dictionary<int, List<GraphQLUploadFileMap>>();

            foreach (var entry in operations.Map)
            {
                var file = forms.Files.GetFile(entry.Key);
                if (file is null)
                {
                    return (null, "File is null");
                }

                foreach (var path in entry.Value)
                {
                    (var index, var parts) = GetParts(path, operations.Batch != default);

                    if (!metaLookup.ContainsKey(index))
                    {
                        metaLookup.Add(index, new List<GraphQLUploadFileMap>());
                    }

                    metaLookup[index].Add(new GraphQLUploadFileMap { File = file, Parts = parts });
                }
            }

            if (operations.Batch != default)
            {
                int i = 0;
                requests = operations.Batch
                    .Select(x => CreateGraphQLRequest(x, metaLookup, i++))
                    .ToList();
            }
            else
            {
                var request = CreateGraphQLRequest(operations.Single, metaLookup, 0);
                requests = new List<GraphQLUploadRequest> { request };
            }

            return (requests, null);
        }
    }
}
