using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections;
using System.Globalization;
using System.Text;

namespace GraphQL.Upload.AspNetCore;

public class GraphQLUploadMiddleware<TSchema> : GraphQLHttpMiddleware<TSchema>
    where TSchema : ISchema
{
    private readonly GraphQLUploadOptions _options;
    private readonly IGraphQLTextSerializer _serializer;

    public GraphQLUploadMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLUploadOptions options,
        IHostApplicationLifetime hostApplicationLifetime)
        : base(next, serializer, documentExecuter, serviceScopeFactory, options, hostApplicationLifetime)
    {
        _options = options;
        _serializer = serializer;
    }

    protected override async Task<(GraphQLRequest? SingleRequest, IList<GraphQLRequest?>? BatchRequest)?> ReadPostContentAsync(HttpContext context, RequestDelegate next, string? mediaType, Encoding? sourceEncoding)
    {
        if (!context.Request.HasFormContentType || !_options.ReadFormOnPost)
            return await base.ReadPostContentAsync(context, next, mediaType, sourceEncoding);

        try
        {
            var formCollection = await context.Request.ReadFormAsync(context.RequestAborted);
            var deserializationResult = _serializer.Deserialize<IList<GraphQLRequest?>>(formCollection["operations"]);
            if (deserializationResult == null)
                return (null, null);

            var map = _serializer.Deserialize<Dictionary<string, string?[]>>(formCollection["map"]);
            if (map != null)
                ApplyMapToRequests(map, formCollection, deserializationResult);

            if (deserializationResult is GraphQLRequest[] array && array.Length == 1)
                return (deserializationResult[0], null);
            else
                return (null, deserializationResult);
        }
        catch (GraphQLUploadError ex)
        {
            await WriteErrorResponseAsync(context, ex.HttpStatusCode, ex);
            return null;
        }
        catch (Exception ex)
        {
            if (!await HandleDeserializationErrorAsync(context, next, ex))
                throw;
            return null;
        }

        void ApplyMapToRequests(Dictionary<string, string?[]> map, IFormCollection form, IList<GraphQLRequest?> requests)
        {
            if (_options.MaximumFileCount.HasValue && form.Files.Count > _options.MaximumFileCount.Value)
                throw new TooManyFilesError();

            foreach (var file in form.Files)
            {
                if (_options.MaximumFileSize.HasValue && _options.MaximumFileSize.Value < file.Length)
                    throw new FileSizeExceededError();
            }

            try
            {
                foreach (var entry in map)
                {
                    var file = form.Files[entry.Key];
                    if (file == null) continue;
                    foreach (var target in entry.Value)
                    {
                        if (target != null)
                            ApplyFileToRequests(file, target, requests);
                    }
                }
            }
            catch (ExecutionError)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BadMapPathError(ex);
            }
        }

        void ApplyFileToRequests(IFormFile file, string target, IList<GraphQLRequest?> requests)
        {
            if (target.StartsWith("variables.", StringComparison.Ordinal))
            {
                if (requests.Count < 1)
                    throw new BadMapPathError();
                ApplyFileToRequest(file, target.Substring(10), requests[0]);
                return;
            }
            var i = target.IndexOf('.');
            if (i == -1 || !string.Equals(target.Substring(i + 1, 10), "variables.", StringComparison.Ordinal))
                throw new BadMapPathError();
            if (!int.TryParse(target.Substring(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                throw new BadMapPathError();
            if (requests.Count < (index + 1))
                throw new BadMapPathError();
            ApplyFileToRequest(file, target.Substring(10 + i + 1), requests[index]);
        }

        void ApplyFileToRequest(IFormFile file, string target, GraphQLRequest? request)
        {
            var inputs = new Dictionary<string, object?>(request?.Variables ?? throw new BadMapPathError());
            object parent = inputs;
            string? prop = null;
            foreach (var location in target.Split('.'))
            {
                if (prop == null)
                {
                    prop = location;
                    continue;
                }
                if (parent is IList list)
                {
                    parent = list[int.Parse(prop)] ?? throw new BadMapPathError();
                }
                else if (parent is IReadOnlyDictionary<string, object?> dic)
                {
                    parent = dic[prop] ?? throw new BadMapPathError();
                }
                else
                {
                    throw new BadMapPathError();
                }
                prop = location;
            }

            // verify that the target is valid
            if (prop == null || prop.Length == 0)
                throw new BadMapPathError();

            // set the target to the form file
            if (parent is IList list2)
            {
                if (list2[int.Parse(prop)] != null)
                    throw new BadMapPathError();
                list2[int.Parse(prop)] = file;
            }
            else if (parent is IDictionary<string, object?> dic)
            {
                if (dic[prop] != null)
                    throw new BadMapPathError();
                dic[prop] = file;
            }
            else
            {
                throw new BadMapPathError();
            }

            // set inputs
            request!.Variables = new Inputs(inputs);
        }
    }
}
