using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Upload.AspNetCore.Tests
{
    public class TestSchema : Schema
    {
        public TestSchema()
        {
            Query = new Query();
            Mutation = new Mutation();
            RegisterValueConverter(new FormFileConverter());
        }
    }

    public class Query : ObjectGraphType
    {
    }

    public class Mutation : ObjectGraphType
    {
        public Mutation()
        {
            Field<NonNullGraphType<StringGraphType>>(
                "singleUpload",
                arguments: new QueryArguments(
                    new QueryArgument<UploadGraphType> { Name = "file" }),
                resolve: context =>
                {
                    var file = context.GetArgument<IFormFile>("file");
                    return file.FileName;
                });

            Field<NonNullGraphType<StringGraphType>>(
                "multipleUpload",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<UploadGraphType>> { Name = "files" }),
                resolve: context =>
                {
                    var files = context.GetArgument<List<IFormFile>>("files");
                    return string.Join(",", files.Select(file => file.FileName));
                });
        }

    }
}
