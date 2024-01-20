using GraphQL.Types;
using GraphQL.Upload.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding <see cref="GraphQLUploadMiddleware{TSchema}"/> to an application.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="GraphQLUploadMiddleware{TSchema}"/> to handle file uploads in GraphQL requests.
    /// </summary>
    /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint which defaults to '/graphql'</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQLUpload<TSchema>(this IApplicationBuilder builder, string path = "/graphql", Action<GraphQLUploadOptions>? configureOptions = null)
        where TSchema : ISchema
    {
        return builder.UseGraphQLUpload<TSchema>(new PathString(path), configureOptions);
    }

    /// <summary>
    /// Adds the <see cref="GraphQLUploadMiddleware{TSchema}"/> to handle file uploads in GraphQL requests.
    /// </summary>
    /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint</param>
    /// <param name="configureOptions">A delegate that is used to configure the <see cref="GraphQLUploadOptions"/>, which are passed to the <see cref="GraphQLUploadMiddleware{TSchema}"/></param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>>
    public static IApplicationBuilder UseGraphQLUpload<TSchema>(this IApplicationBuilder builder, PathString path, Action<GraphQLUploadOptions>? configureOptions = null)
        where TSchema : ISchema
    {
        var options = new GraphQLUploadOptions();
        configureOptions?.Invoke(options);

        return builder.UseGraphQL<GraphQLUploadMiddleware<TSchema>>(path, options);
    }
}