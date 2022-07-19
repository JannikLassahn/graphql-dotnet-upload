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

            var options = new GraphQLUploadOptions
            {
                MaximumFileCount = 0
            };

            using (var server = CreateServer(options))
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

            var options = new GraphQLUploadOptions
            {
                MaximumFileSize = 2
            };

            using (var server = CreateServer(options))
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
