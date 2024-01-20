using GraphQL.DI;
using GraphQL.Upload.AspNetCore;

namespace GraphQL;

public static class GraphQLUploadExtensions
{
    public static IGraphQLBuilder AddGraphQLUpload(this IGraphQLBuilder builder)
    {
        builder.Services.Register<UploadGraphType>(ServiceLifetime.Singleton);
        
        return builder;
    }
}
