﻿using GraphQL.Upload.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLUpload(this IServiceCollection services)
        {
            services.AddSingleton<GraphQLUploadRequestDeserializer>();
            services.AddSingleton<UploadGraphType>();
            
            return services;
        }
    }
}
