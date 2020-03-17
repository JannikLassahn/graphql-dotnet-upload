using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Upload.AspNetCore
{
    public class GraphQLUploadFileMap
    {
        public List<object> Parts { get; set; }
        public IFormFile File { get; set; }
    }
}