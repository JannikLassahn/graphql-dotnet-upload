using System.Net;

namespace GraphQL.Upload.AspNetCore;

public class GraphQLUploadError : ExecutionError
{
    public HttpStatusCode HttpStatusCode { get; }

    public GraphQLUploadError(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = statusCode;
    }
}

public class BadMapPathError : GraphQLUploadError
{
    public BadMapPathError(Exception? innerException = null)
        : base("Invalid map path." + (innerException != null ? " " + innerException.Message : null), HttpStatusCode.BadRequest, innerException)
    {
    }
}

public class TooManyFilesError : GraphQLUploadError
{
    public TooManyFilesError()
        : base("File uploads exceeded.", HttpStatusCode.RequestEntityTooLarge)
    {
    }
}

public class FileSizeExceededError : GraphQLUploadError
{
    public FileSizeExceededError()
        : base("File size limit exceeded.", HttpStatusCode.RequestEntityTooLarge)
    {
    }
}
