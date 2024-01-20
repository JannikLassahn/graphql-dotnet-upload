﻿using GraphQL;
using GraphQL.Types;

namespace FileUploadSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<UploadRepository>();

            services.AddGraphQL(builder => builder
                .AddSchema<SampleSchema>()
                .AddGraphTypes()
                .AddGraphQLUpload()
                .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = true)
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
