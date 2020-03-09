using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Upload.AspNetCore.NewtonsoftJson
{
    internal sealed class InternalGraphQLUploadRequest
    {
        [JsonProperty(GraphQLUploadRequest.QUERY_KEY)]
        public string Query { get; set; }

        [JsonProperty(GraphQLUploadRequest.VARIABLES_KEY)]
        public JObject Variables { get; set; }

        [JsonProperty(GraphQLUploadRequest.OPERATION_NAME_KEY)]
        public string OperationName { get; set; }
    }
}
