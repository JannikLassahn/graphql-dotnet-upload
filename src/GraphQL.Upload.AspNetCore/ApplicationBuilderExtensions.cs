using GraphQL.Types;
using GraphQL.Upload.AspNetCore;
using Microsoft.AspNetCore.Http;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding <see cref="GraphQLUploadMiddleware{TSchema}"/> to an application.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="GraphQLUploadMiddleware{TSchema}"/> to handle file uploads in GraphQL requests.
        /// </summary>
        public static IApplicationBuilder UseGraphQLUpload<TSchema>(this IApplicationBuilder builder)
            where TSchema : ISchema
        {
            return builder.UseGraphQLUpload<TSchema>("/graphql", new GraphQLUploadOptions());
        }

        /// <summary>
        /// Adds the <see cref="GraphQLUploadMiddleware{TSchema}"/> to handle file uploads in GraphQL requests.
        /// </summary>
        public static IApplicationBuilder UseGraphQLUpload<TSchema>(this IApplicationBuilder builder, PathString path, Action<GraphQLUploadOptions> configure)
            where TSchema : ISchema
        {
            var options = new GraphQLUploadOptions();
            configure(options);

            return builder.UseGraphQLUpload<TSchema>(path, options);
        }

        /// <summary>
        /// Adds the <see cref="GraphQLUploadMiddleware{TSchema}"/> to handle file uploads in GraphQL requests.
        /// </summary>
        public static IApplicationBuilder UseGraphQLUpload<TSchema>(this IApplicationBuilder builder, PathString path, GraphQLUploadOptions options)
            where TSchema : ISchema
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            return builder.UseWhen(context => context.Request.Path.StartsWithSegments(path), branch => branch.UseMiddleware<GraphQLUploadMiddleware<ISchema>>(options));
        }
    }
}
