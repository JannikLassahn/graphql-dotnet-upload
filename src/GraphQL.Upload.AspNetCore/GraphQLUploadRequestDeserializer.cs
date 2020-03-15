using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;

#if IS_NET_CORE_3_ONWARDS_TARGET
using GraphQL.SystemTextJson;
using System.Text.Json;
#else
using GraphQL.NewtonsoftJson;
using Newtonsoft.Json;
#endif


namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadRequestDeserializer
    {
#if IS_NET_CORE_3_ONWARDS_TARGET
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions();

        public GraphQLUploadRequestDeserializer()
        {
            // Add converter that deserializes Variables property
            _serializerOptions.Converters.Add(new ObjectDictionaryConverter());
        }
#endif

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
#if IS_NET_CORE_3_ONWARDS_TARGET
                    result.Batch = (JsonSerializer.Deserialize<InternalGraphQLUploadRequest[]>(operations, _serializerOptions))
                        .Select(ToGraphQLRequest)
                        .ToArray();
#else
                    result.Batch = (JsonConvert.DeserializeObject<InternalGraphQLUploadRequest[]>(operations))
                        .Select(ToGraphQLRequest)
                        .ToArray();
#endif
                }
                else
                {
#if IS_NET_CORE_3_ONWARDS_TARGET
                    result.Single = ToGraphQLRequest(JsonSerializer.Deserialize<InternalGraphQLUploadRequest>(operations, _serializerOptions));
#else
                    result.Single = ToGraphQLRequest(JsonConvert.DeserializeObject<InternalGraphQLUploadRequest>(operations));
#endif
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
#if IS_NET_CORE_3_ONWARDS_TARGET
                Variables = graphQLUploadRequest.Variables,
#else
                Variables = graphQLUploadRequest.Variables.ToDictionary(),
#endif
            };

        private void SetMap(GraphQLUploadRequestDeserializationResult result, IFormCollection form)
        {
            if (!form.TryGetValue("map", out var map))
            {
                throw new Exception("Missing field 'map'");
            }

            try
            {
#if IS_NET_CORE_3_ONWARDS_TARGET
                result.Map = JsonSerializer.Deserialize<Dictionary<string, string[]>>(map);
#else
                result.Map = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(map);
#endif
            }
            catch (JsonException)
            {
                throw new Exception("Invalid JSON in the 'map' Upload field.");
            }
        }
    }
}
