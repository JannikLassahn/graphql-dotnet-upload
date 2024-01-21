using Xunit;

namespace GraphQL.Upload.AspNetCore.Tests
{
    public class MiddlewareOptionsTests : TestBase
    {
        [Fact]
        public async Task TooManyFiles()
        {
            // Arrange
            var operations = new StringContent(@"{""query"": ""mutation($file: Upload) { singleUpload(file: $file) }"", ""variables"": {""file"": null } }");
            var map = new StringContent(@"{ ""0"": [""variables.file""] }");

            var fileA = CreatePlainTextFile("test");

            var multipartContent = new MultipartFormDataContent
            {
                { operations, "operations" },
                { map, "map" },
                { fileA, "0", "a.txt" }
            };

            using (var server = CreateServer(options => options.MaximumFileCount = 0))
            {
                // Act
                var client = server.CreateClient();
                var response = await client.PostAsync("/graphql", multipartContent);

                // Assert
                Assert.Equal(System.Net.HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
            }
        }

        [Fact]
        public async Task TooLargeFile()
        {
            // Arrange
            var operations = new StringContent(@"{""query"": ""mutation($file: Upload) { singleUpload(file: $file) }"", ""variables"": {""file"": null } }");
            var map = new StringContent(@"{ ""0"": [""variables.file""] }");

            var fileA = CreatePlainTextFile("t");
            var fileB = CreatePlainTextFile("test");

            var multipartContent = new MultipartFormDataContent
            {
                { operations, "operations" },
                { map, "map" },
                { fileA, "0", "a.txt" },
                { fileB, "1", "b.txt" }
            };

            using (var server = CreateServer(options => options.MaximumFileSize = 2))
            {
                // Act
                var client = server.CreateClient();
                var response = await client.PostAsync("/graphql", multipartContent);

                // Assert
                Assert.Equal(System.Net.HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
            }
        }
    }
}
