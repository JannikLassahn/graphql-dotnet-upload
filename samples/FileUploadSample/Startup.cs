using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using GraphQL.Upload.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FileUploadSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));

            services.AddSingleton<UploadGraphType>();

            services.AddSingleton<ISchema, SampleSchema>();
            services.AddSingleton<Query>();
            services.AddSingleton<Mutation>();
            services.AddSingleton<FileGraphType>();

            services.AddGraphQL(_ =>
            {
                _.ExposeExceptions = true;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<GraphQLMultipartMiddleware<ISchema>>(new PathString("/graphql"));
            app.UseGraphQL<ISchema>();
            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions
            {
                Path = "/"
            });
        }
    }
}
