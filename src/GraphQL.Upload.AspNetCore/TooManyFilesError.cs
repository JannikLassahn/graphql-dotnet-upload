using System.Net;

namespace GraphQL.Upload.AspNetCore;

/// <summary>
/// Represents an error when too many files are uploaded in a GraphQL request.
/// </summary>
public class TooManyFilesError : GraphQLUploadError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooManyFilesError"/> class.
    /// </summary>
    public TooManyFilesError()
        : base("File uploads exceeded.", HttpStatusCode.RequestEntityTooLarge)
    {
    }
}
