using GraphQL;
using GraphQL.Types;
using GraphQL.Upload.AspNetCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileUploadSample
{
    public class SampleSchema : Schema
    {
        public SampleSchema(IDependencyResolver resolver)
            : base(resolver)
        {
            Query = resolver.Resolve<Query>();
            Mutation = resolver.Resolve<Mutation>();
        }
    }

    public class Query : ObjectGraphType
    {
        public Query(UploadRepository uploads)
        {
            Field<ListGraphType<FileGraphType>>("uploads", resolve: ctx => uploads.Files);
        }
    }

    public class Mutation : ObjectGraphType
    {
        public Mutation(UploadRepository uploads)
        {
            Field<FileGraphType>(
                "singleUpload",
                arguments: new QueryArguments(
                    new QueryArgument<UploadGraphType> { Name = "file" }),
                resolve: context =>
                {
                    var file = context.GetArgument<IFormFile>("file");
                    return uploads.Save(file);
                });

            Field<ListGraphType<FileGraphType>>(
                "multipleUpload",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<UploadGraphType>> { Name = "files" }),
                resolve: context =>
                {
                    var files = context.GetArgument<IEnumerable<IFormFile>>("files");
                    return Task.WhenAll(files.Select(file => uploads.Save(file)));
                });
        }
    }

    public class File
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string MimeType { get; set; }
        public string Path { get; set; }
    }

    public class FileGraphType : ObjectGraphType<File>
    {
        public FileGraphType()
        {
            Field(f => f.Id).Name("id");
            Field(f => f.Name).Name("filename");
            Field(f => f.MimeType).Name("mimetype");
            Field(f => f.Path).Name("path");
        }
    }
}
