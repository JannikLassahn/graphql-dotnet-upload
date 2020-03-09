using Microsoft.AspNetCore.Http;

namespace GraphQL.Upload.AspNetCore
{
    public interface IGraphQLUploadRequestDeserializer
    {
        GraphQLUploadRequestDeserializationResult DeserializeFromFormCollection(IFormCollection form);
    }
}