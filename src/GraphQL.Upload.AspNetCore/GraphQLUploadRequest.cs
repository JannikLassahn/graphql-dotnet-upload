using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadRequest
    {
        public const string OPERATION_NAME_KEY = "operationName";
        public const string QUERY_KEY = "query";
        public const string VARIABLES_KEY = "variables";

        [JsonPropertyName(GraphQLUploadRequest.OPERATION_NAME_KEY)]
        public string OperationName { get; set; }

        [JsonPropertyName(GraphQLUploadRequest.QUERY_KEY)]
        public string Query { get; set; }

        [JsonPropertyName(GraphQLUploadRequest.VARIABLES_KEY)]
        public Inputs Variables { get; set; }

        public List<GraphQLUploadFileMap> TokensToReplace { get; set; }

        public Inputs GetVariables()
        {
            var variables = new Dictionary<string, object>(Variables);

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
