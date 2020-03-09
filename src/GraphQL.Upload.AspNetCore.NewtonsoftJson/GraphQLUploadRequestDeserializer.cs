using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Upload.AspNetCore.NewtonsoftJson
{
    public class GraphQLUploadRequestDeserializer : IGraphQLUploadRequestDeserializer
    {
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
                    result.Batch = (JsonConvert.DeserializeObject<InternalGraphQLUploadRequest[]>(operations))
                        .Select(ToGraphQLRequest)
                        .ToArray();
                }
                else
                {
                    result.Single = ToGraphQLRequest(JsonConvert.DeserializeObject<InternalGraphQLUploadRequest>(operations));
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
                Variables = graphQLUploadRequest.Variables.ToDictionary(),
            };

        private void SetMap(GraphQLUploadRequestDeserializationResult result, IFormCollection form)
        {
            if (!form.TryGetValue("map", out var map))
            {
                throw new Exception("Missing field 'map'");
            }

            try
            {
                result.Map = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(map);
            }
            catch (JsonException)
            {
                throw new Exception("Invalid JSON in the 'map' Upload field.");
            }
        }
    }
}
