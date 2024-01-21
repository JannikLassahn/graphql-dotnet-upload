using System.Net;

namespace GraphQL.Upload.AspNetCore;

/// <summary>
/// Represents an error when a file exceeds the allowed size limit in a GraphQL upload.
/// </summary>
public class FileSizeExceededError : GraphQLUploadError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSizeExceededError"/> class.
    /// </summary>
    public FileSizeExceededError()
        : base("File size limit exceeded.", HttpStatusCode.RequestEntityTooLarge)
    {
    }
}
