﻿using GraphQL.SystemTextJson;
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
        public TestServer CreateServer(GraphQLUploadOptions options = null)
        {
            var path = Assembly.GetAssembly(typeof(TestBase)).Location;

            var hostBuilder = new WebHostBuilder()
                .UseContentRoot(Path.GetDirectoryName(path))
                .ConfigureServices(services =>
                    services.AddSingleton<IDocumentExecuter, DocumentExecuter>()
                            .AddSingleton<IGraphQLTextSerializer, GraphQLSerializer>()
                            .AddSingleton<IGraphQLSerializer, GraphQLSerializer>()
                            .AddSingleton<ISchema, TestSchema>()
                            .AddGraphQLUpload()
                )
                .Configure(app =>
                    app.UseGraphQLUpload<ISchema>("/graphql", options ?? new GraphQLUploadOptions())
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
