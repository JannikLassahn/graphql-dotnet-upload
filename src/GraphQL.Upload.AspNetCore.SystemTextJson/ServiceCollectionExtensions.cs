using System;
using System.Text.Json;
using GraphQL.Upload.AspNetCore;
using GraphQL.Upload.AspNetCore.SystemTextJson;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLUpload(this IServiceCollection services,
            Action<JsonSerializerOptions> jsonSerializerOptions = null)
        {
            services.AddSingleton<IGraphQLUploadRequestDeserializer>(x => new GraphQLUploadRequestDeserializer(jsonSerializerOptions));
            services.AddSingleton<UploadGraphType>();
            
            return services;
        }
    }
}
