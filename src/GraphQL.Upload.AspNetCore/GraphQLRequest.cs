using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace GraphQL.Upload.AspNetCore
{
    internal class GraphQLRequest
    {
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";
        public const string OperationNameKey = "operationName";
        public const string MapKey = "map";

        [JsonProperty(QueryKey)]
        public string Query { get; set; }

        [JsonProperty(VariablesKey)]
        public JObject Variables { get; set; }

        [JsonProperty(OperationNameKey)]
        public string OperationName { get; set; }

        [JsonIgnore]
        public List<Meta> TokensToReplace { get; set; }

        public Inputs GetInputs()
        {
            var variables = Variables?.ToInputs();

            // the following implementation seems brittle because of a lot of casting
            // and it depends on the types that ToInputs() creates.

            foreach (var info in TokensToReplace)
            {
                int i = 0;
                object o = variables;

                foreach (var p in info.Parts)
                {
                    var isLast = i++ == info.Parts.Count - 1;

                    if (p is string s)
                    {
                        if (isLast)
                            ((Inputs)o)[s] = info.File;
                        else
                            o = ((Inputs)o)[s];
                    }
                    else if (p is int index)
                    {
                        if (isLast)
                            ((List<object>)o)[index] = info.File;
                        else
                            o = ((List<object>)o)[index];
                    }
                }
            }

            return variables;
        }
    }
}
