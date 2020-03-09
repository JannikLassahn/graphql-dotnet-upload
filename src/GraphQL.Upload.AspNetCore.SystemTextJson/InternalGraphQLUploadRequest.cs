using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GraphQL.Upload.AspNetCore.SystemTextJson
{
    internal sealed class InternalGraphQLUploadRequest
    {
        [JsonPropertyName(GraphQLUploadRequest.QUERY_KEY)]
        public string Query { get; set; }

        [JsonPropertyName(GraphQLUploadRequest.VARIABLES_KEY)]
        public Dictionary<string, object> Variables { get; set; }

        [JsonPropertyName(GraphQLUploadRequest.OPERATION_NAME_KEY)]
        public string OperationName { get; set; }
    }
}
