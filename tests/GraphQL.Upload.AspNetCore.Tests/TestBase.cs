using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text;

namespace GraphQL.Upload.AspNetCore.Tests
{
    public abstract class TestBase
    {
        public TestServer CreateServer(Action<GraphQLUploadOptions>? options = null)
        {
            var path = Assembly.GetCallingAssembly().Location;

            var hostBuilder = new WebHostBuilder()
                .UseContentRoot(Path.GetDirectoryName(path)!)
                .ConfigureServices(services =>
                    services.AddGraphQL(b => b
                        .AddSchema<TestSchema>()
                        .AddSystemTextJson()
                        .AddGraphQLUpload())
                )
                .Configure(app =>
                    app.UseGraphQLUpload<ISchema>("/graphql", options)
                );

            return new TestServer(hostBuilder);
        }

        protected static ByteArrayContent CreatePlainTextFile(string fileContent)
        {
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(fileContent));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            content.Headers.ContentLength = Encoding.UTF8.GetByteCount(fileContent);
            return content;
        }
    }
}
