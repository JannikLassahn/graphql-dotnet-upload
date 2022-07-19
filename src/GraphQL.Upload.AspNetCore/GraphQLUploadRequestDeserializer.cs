using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadRequestDeserializer
    {
        private readonly IGraphQLTextSerializer _graphQLTextSerializer;

        public GraphQLUploadRequestDeserializer(IGraphQLTextSerializer graphQLTextSerializer)
        {
            _graphQLTextSerializer = graphQLTextSerializer;
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
                    result.Batch = _graphQLTextSerializer.Deserialize<GraphQLUploadRequest[]>(operations)
                        .ToArray();
                }
                else
                {
                    result.Single = _graphQLTextSerializer.Deserialize<GraphQLUploadRequest>(operations);
                }
            }
            catch
            {
                throw new Exception("Invalid JSON in the 'operations' Upload field.");
            }
        }

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
