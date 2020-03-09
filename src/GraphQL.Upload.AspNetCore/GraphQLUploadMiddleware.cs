using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadMiddleware<TSchema>
        where TSchema : ISchema
    {
        private const string DOCS_URL = "See: https://github.com/jaydenseric/graphql-Upload-request-spec.";
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly GraphQLUploadOptions _options;
        private readonly IGraphQLUploadRequestDeserializer _requestDeserializer;

        public GraphQLUploadMiddleware(ILogger<GraphQLUploadMiddleware<TSchema>> logger, RequestDelegate next,
            GraphQLUploadOptions options, IGraphQLUploadRequestDeserializer requestDeserializer)
        {
            _logger = logger;
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options;
            _requestDeserializer = requestDeserializer;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.HasFormContentType)
            {
                await _next(context);
                return;
            }

            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            // Inspiration: https://github.com/graphql/express-graphql/blob/master/src/index.js
            var httpRequest = context.Request;
            var httpResponse = context.Response;

            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();
            var cancellationToken = GetCancellationToken(context);

            // GraphQL HTTP only supports GET and POST methods
            bool isPost = HttpMethods.IsPost(httpRequest.Method);
            if (!isPost)
            {
                httpResponse.Headers["Allow"] = "POST";
                await WriteErrorResponseAsync(httpResponse, writer,
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
            catch(Exception exception)
            {
                await WriteErrorResponseAsync(httpResponse, writer, $"{exception.Message} ${DOCS_URL}", statusCode);
                return;
            }

            (files, error, statusCode) = GetFiles(form);
            if (error != null)
            {
                await WriteErrorResponseAsync(httpResponse, writer, error, statusCode);
                return;
            }

            (requests, error) = ExtractGraphQLRequests(uploadRequest, form);
            if(error != null)
            {
                await WriteErrorResponseAsync(httpResponse, writer, error, statusCode);
                return;
            }

            var executer = context.RequestServices.GetRequiredService<IDocumentExecuter>();
            var schema = context.RequestServices.GetRequiredService<TSchema>();

            var test = requests.First().Variables.ToInputs();

            var results = await Task.WhenAll(
                requests.Select(request => executer.ExecuteAsync(new ExecutionOptions
                {
                    CancellationToken = context.RequestAborted,
                    Schema = schema,
                    Query = request.Query,
                    OperationName = request.OperationName,
                    Inputs = request.GetInputs(),
                    UserContext = _options.UserContextFactory?.Invoke(context),
                })));

            await WriteResponseAsync(context, writer, results);
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

        private Task WriteErrorResponseAsync(HttpResponse httpResponse, IDocumentWriter writer,
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

            return writer.WriteAsync(httpResponse.Body, result);
        }

        private async Task WriteResponseAsync(HttpContext context, IDocumentWriter writer, ExecutionResult[] results)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;

            foreach(var result in results)
            {
                if (result.Errors != null)
                {
                    _logger.LogError("GraphQL execution error(s): {Errors}", result.Errors);
                }
            }

            if (results.Length == 1)
            {
                await writer.WriteAsync(context.Response.Body, results[0]);
                return;
            }

            await writer.WriteAsync(context.Response.Body, results);
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
