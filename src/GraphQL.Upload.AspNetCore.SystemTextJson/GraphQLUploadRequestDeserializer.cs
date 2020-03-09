using GraphQL.SystemTextJson;
using Microsoft.AspNetCore.Http;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GraphQL.Upload.AspNetCore.SystemTextJson
{
    public class GraphQLUploadRequestDeserializer : IGraphQLUploadRequestDeserializer
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions();

        public GraphQLUploadRequestDeserializer(Action<JsonSerializerOptions> configure)
        {
            // Add converter that deserializes Variables property
            _serializerOptions.Converters.Add(new ObjectDictionaryConverter());

            configure?.Invoke(_serializerOptions);
        }

        public GraphQLUploadRequestDeserializationResult DeserializeFromFormCollection(IFormCollection form)
        {
            var result = new GraphQLUploadRequestDeserializationResult { IsSuccessful = true };

            SetOperations(result, form);
            SetMap(result, form);

            return result;
        }

        private void SetOperations(GraphQLUploadRequestDeserializationResult result, IFormCollection form)
        {
            if (!form.TryGetValue("operations", out var operations))
            {
                throw new Exception("Missing field 'operations'.");
            }

            var firstChar = operations[0][0];
            var isBatched = false;

            if (firstChar == '[')
            {
                isBatched = true;
            }

            try
            {
                if (isBatched)
                {
                    result.Batch = (JsonSerializer.Deserialize<InternalGraphQLUploadRequest[]>(operations, _serializerOptions))
                        .Select(ToGraphQLRequest)
                        .ToArray();
                }
                else
                {
                    result.Single = ToGraphQLRequest(JsonSerializer.Deserialize<InternalGraphQLUploadRequest>(operations, _serializerOptions));
                }
            }
            catch
            {
                throw new Exception("Invalid JSON in the 'operations' Upload field.");
            }
        }

        private GraphQLUploadRequest ToGraphQLRequest(InternalGraphQLUploadRequest graphQLUploadRequest)
            => new GraphQLUploadRequest
            {
                OperationName = graphQLUploadRequest.OperationName,
                Query = graphQLUploadRequest.Query,
                Variables = graphQLUploadRequest.Variables,
            };

        private void SetMap(GraphQLUploadRequestDeserializationResult result, IFormCollection form)
        {
            if (!form.TryGetValue("map", out var map))
            {
                throw new Exception("Missing field 'map'");
            }

            try
            {
                result.Map = JsonSerializer.Deserialize<Dictionary<string, string[]>>(map);
            }
            catch (JsonException)
            {
                throw new Exception("Invalid JSON in the 'map' Upload field.");
            }
        }
    }
}
