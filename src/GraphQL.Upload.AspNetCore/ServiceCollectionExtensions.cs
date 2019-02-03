using GraphQL.Upload.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLUpload(this IServiceCollection services)
        {
            return services.AddSingleton<UploadGraphType>();
        }
    }
}
