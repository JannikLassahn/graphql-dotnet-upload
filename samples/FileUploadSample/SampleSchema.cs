
using GraphQL;
using GraphQL.Types;
using GraphQL.Upload.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace FileUploadSample
{
    public class SampleSchema : Schema
    {
        public SampleSchema(IDependencyResolver resolver)
            : base(resolver)
        {
            RegisterValueConverter(new FormFileConverter());

            Query = resolver.Resolve<Query>();
            Mutation = resolver.Resolve<Mutation>();
        }
    }

    public class Query : ObjectGraphType
    {

    }

    public class Mutation : ObjectGraphType
    {
        public Mutation()
        {
            Field<FileGraphType>(
                "singleUpload",
                arguments: new QueryArguments(
                    new QueryArgument<UploadGraphType> { Name = "file" }),
                resolve: context =>
                {
                    var file = context.GetArgument<IFormFile>("file");
                    return new File { Name = file.FileName, ContentType = file.ContentType };
                });
        }

    }

    public class File
    {
        public string Name { get; set; }
        public string ContentType { get; set; }
    }

    public class FileGraphType : ObjectGraphType<File>
    {
        public FileGraphType()
        {
            Field(f => f.Name);
            Field(f => f.ContentType);
        }
    }
}
