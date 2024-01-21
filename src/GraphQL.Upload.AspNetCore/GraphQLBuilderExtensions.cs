using GraphQL.DI;
using GraphQL.Upload.AspNetCore;

namespace GraphQL;

/// <summary>
/// Provides extension methods for setting up GraphQL file upload support in an application.
/// </summary>
public static class GraphQLUploadExtensions
{
    /// <summary>
    /// Registers <see cref="UploadGraphType"/> within the dependency injection framework
    /// as a singleton for use within a GraphQL schema.
    /// </summary>
    public static IGraphQLBuilder AddGraphQLUpload(this IGraphQLBuilder builder)
    {
        builder.Services.Register<UploadGraphType>(ServiceLifetime.Singleton);

        return builder;
    }
}
