using System.Collections.Generic;
using System.Linq;

#if IS_NET_CORE_3_ONWARDS_TARGET
using System.Text.Json.Serialization;
using GraphQL.SystemTextJson;

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
        [JsonConverter(typeof(InputsJsonConverter))]
        public Inputs Variables { get; set; }
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
#if IS_NET_CORE_3_ONWARDS_TARGET
                            var newInputs = ((Inputs)variableSection).ToDictionary(pair => pair.Key, pair => pair.Value);
                            newInputs[key] = info.File;
                            variables = new Inputs(newInputs);
#else
                            ((Dictionary<string, object>)variableSection)[key] = info.File;
#endif
                        }
                        else
                        {
#if IS_NET_CORE_3_ONWARDS_TARGET
                            variableSection = ((Inputs)variableSection)[key];
#else

                            variableSection = ((Dictionary<string, object>)variableSection)[key];
#endif
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
#if IS_NET_CORE_3_ONWARDS_TARGET
            return variables;
#else
            return variables.ToInputs();
#endif
        }
    }
}
