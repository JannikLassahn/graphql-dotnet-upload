using GraphQL;
using GraphQL.Http;
using GraphQL.Types;
using GraphQL.Upload.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace GraphQL.Upload.AspNetCore.Tests
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>()
                    .AddSingleton<IDocumentWriter, DocumentWriter>()
                    .AddSingleton<ISchema, TestSchema>()
                    .AddSingleton<UploadGraphType>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<GraphQLMultipartMiddleware<ISchema>>(new PathString("/graphql"));
        }
    }

    public abstract class TestBase
    {
        public TestServer CreateServer()
        {
            var path = Assembly.GetAssembly(typeof(TestBase))
               .Location;

            var hostBuilder = new WebHostBuilder()
                .UseContentRoot(Path.GetDirectoryName(path))
                .UseStartup<TestStartup>();

            return new TestServer(hostBuilder);
        }
    }
}
