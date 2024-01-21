using System.Net;

namespace GraphQL.Upload.AspNetCore;

/// <summary>
/// Represents an error when an invalid map path is provided in a GraphQL file upload request.
/// </summary>
public class BadMapPathError : GraphQLUploadError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BadMapPathError"/> class.
    /// </summary>
    /// <param name="innerException">The inner exception, if any, that caused the bad map path error.</param>
    public BadMapPathError(Exception? innerException = null)
        : base("Invalid map path." + (innerException != null ? " " + innerException.Message : null), HttpStatusCode.BadRequest, innerException)
    {
    }
}
