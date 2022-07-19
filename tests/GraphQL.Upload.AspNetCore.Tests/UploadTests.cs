using Xunit;

namespace GraphQL.Upload.AspNetCore.Tests
{
    public class UploadTests : TestBase
    {
        [Fact]
        public async Task UploadSingleFile()
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

            using (var server = CreateServer())
            {
                // Act
                var client = server.CreateClient();
                var response = await client.PostAsync("/graphql", multipartContent);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Contains(@"{""data"":{""singleUpload"":""a.txt""}}", await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task UploadMutlipleFiles()
        {
            // Arrange
            var operations = new StringContent(@"{""query"": ""mutation($files: [Upload]) { multipleUpload(files: $files) }"", ""variables"": {""files"": [null, null] } }");
            var map = new StringContent(@"{ ""0"": [""variables.files.0""], ""1"":[""variables.files.1""] }");

            var fileA = CreatePlainTextFile("test");
            var fileB = CreatePlainTextFile("test");

            var multipartContent = new MultipartFormDataContent
            {
                { operations, "operations" },
                { map, "map" },
                { fileA, "0", "a.txt" },
                { fileB, "1", "b.txt" }
            };

            using (var server = CreateServer())
            {
                // Act
                var client = server.CreateClient();
                var response = await client.PostAsync("/graphql", multipartContent);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Contains(@"{""data"":{""multipleUpload"":""a.txt,b.txt""}}", await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task UploadMutlipleFilesWithBatching()
        {
            // Arrange
            var operations = new StringContent(@"[
                {""query"": ""mutation($file: Upload) { singleUpload(file: $file) }"", ""variables"": {""file"": null } }, 
                {""query"": ""mutation($files: [Upload]) { multipleUpload(files: $files) }"", ""variables"": {""files"": [null, null] } }]");
            var map = new StringContent(@"{ ""0"": [""0.variables.file""], ""1"":[""1.variables.files.0""], ""2"":[""1.variables.files.1""] }");

            var fileA = CreatePlainTextFile("test");
            var fileB = CreatePlainTextFile("test");
            var fileC = CreatePlainTextFile("test");

            var multipartContent = new MultipartFormDataContent
            {
                { operations, "operations" },
                { map, "map" },
                { fileA, "0", "a.txt" },
                { fileB, "1", "b.txt" },
                { fileC, "2", "c.txt" }
            };

            using (var server = CreateServer())
            {
                // Act
                var client = server.CreateClient();
                var response = await client.PostAsync("/graphql", multipartContent);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Contains(@"[{""data"":{""singleUpload"":""a.txt""}},{""data"":{""multipleUpload"":""b.txt,c.txt""}}]", await response.Content.ReadAsStringAsync());
            }
        }
    }
}
