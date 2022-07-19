using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Upload.AspNetCore.Tests
{
    public class TestSchema : Schema
    {
        public TestSchema()
        {
            Query = new Query();
            Mutation = new Mutation();
        }
    }

    public sealed class Query : ObjectGraphType
    {
        public Query()
        {
            Field<NonNullGraphType<BooleanGraphType>>()
                .Name("dummy")
                .Resolve(x => true);
        }
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
