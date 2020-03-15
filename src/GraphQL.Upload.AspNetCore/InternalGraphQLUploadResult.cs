#if IS_NET_CORE_3_ONWARDS_TARGET
using System.Collections.Generic;
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace GraphQL.Upload.AspNetCore
{
    internal sealed class InternalGraphQLUploadRequest
    {
#if IS_NET_CORE_3_ONWARDS_TARGET
        [JsonPropertyName(GraphQLUploadRequest.OPERATION_NAME_KEY)]
#else
        [JsonProperty(GraphQLUploadRequest.OPERATION_NAME_KEY)]
#endif
        public string OperationName { get; set; }

#if IS_NET_CORE_3_ONWARDS_TARGET
        [JsonPropertyName(GraphQLUploadRequest.QUERY_KEY)]
#else
        [JsonProperty(GraphQLUploadRequest.QUERY_KEY)]
#endif
        public string Query { get; set; }

#if IS_NET_CORE_3_ONWARDS_TARGET
        [JsonPropertyName(GraphQLUploadRequest.VARIABLES_KEY)]
        public Dictionary<string, object> Variables { get; set; }
#else
        [JsonProperty(GraphQLUploadRequest.VARIABLES_KEY)]
        public JObject Variables { get; set; }
#endif
    }
}
