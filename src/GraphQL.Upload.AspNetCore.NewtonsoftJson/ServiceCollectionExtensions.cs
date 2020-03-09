using GraphQL.Upload.AspNetCore;
using GraphQL.Upload.AspNetCore.NewtonsoftJson;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLUpload(this IServiceCollection services)
        {
            services.AddSingleton<IGraphQLUploadRequestDeserializer>(x => new GraphQLUploadRequestDeserializer());
            services.AddSingleton<UploadGraphType>();
            
            return services;
        }
    }
}
