using System.Collections.Generic;
using System.Linq;

#if IS_NET_CORE_3_ONWARDS_TARGET
using System.Text.Json.Serialization;
using System.Text.Json;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadRequest
    {
        public const string OPERATION_NAME_KEY = "operationName";
        public const string QUERY_KEY = "query";
        public const string VARIABLES_KEY = "variables";

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

        public List<GraphQLUploadFileMap> TokensToReplace { get; set; }

        public Inputs GetInputs()
        {
#if IS_NET_CORE_3_ONWARDS_TARGET
            var variables = Variables;
#else
            var variables = Variables.ToDictionary();
#endif

            foreach (var info in TokensToReplace)
            {
                object variableSection = variables;

                for (var i = 0; i < info.Parts.Count; i++)
                {
                    var part = info.Parts[i];
                    var isLast = i == info.Parts.Count - 1;

                    if (part is string key)
                    {
                        if (isLast)
                        {
                            ((Dictionary<string, object>)variableSection)[key] = info.File;
                        }
                        else
                        {
#if IS_NET_CORE_3_ONWARDS_TARGET
                            if (((Dictionary<string, object>)variableSection)[key] is JsonElement jsonElement)
                            {
                                var count = jsonElement.GetArrayLength();
                                var list = Enumerable.Repeat((object)null, count).ToList();
                                ((Dictionary<string, object>)variableSection)[key] = list;
                            }
#endif

                            variableSection = ((Dictionary<string, object>)variableSection)[key];
                        }
                    }
                    else if (part is int index)
                    {
                        if (isLast)
                        {
                            ((List<object>)variableSection)[index] = info.File;
                        }
                        else
                        {
                            variableSection = ((List<object>)variableSection)[index];
                        }
                    }
                }
            }

            return variables.ToInputs();
        }
    }
}
