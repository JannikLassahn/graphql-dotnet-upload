namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadRequestDeserializationResult
    {
        public bool IsSuccessful { get; set; }
        public GraphQLUploadRequest Single { get; set; }
        public GraphQLUploadRequest[] Batch { get; set; }
        public Dictionary<string, string[]> Map { get; set; }
    }
}