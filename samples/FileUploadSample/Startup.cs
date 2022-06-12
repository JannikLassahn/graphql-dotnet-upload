using GraphQL;
using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileUploadSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<UploadRepository>();

            services.AddSingleton<ISchema, SampleSchema>();
            services.AddSingleton<Query>();
            services.AddSingleton<Mutation>();
            services.AddSingleton<FileGraphType>();

            services.AddGraphQLUpload();
            services.AddGraphQL(builder => builder
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
                .AddSystemTextJson());

            services.AddCors();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseCors(b => b
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            // register the middleware that can handle multipart requests first
            app.UseGraphQLUpload<ISchema>();

            app.UseGraphQL<ISchema>();
            app.UseGraphQLPlayground("/");
        }
    }
}
