using System.Net;

namespace GraphQL.Upload.AspNetCore;

/// <summary>
/// Represents errors related to GraphQL file uploads.
/// </summary>
public class GraphQLUploadError : ExecutionError
{
    /// <summary>
    /// Gets the HTTP status code associated with the error.
    /// </summary>
    public HttpStatusCode HttpStatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLUploadError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code (defaults to BadRequest).</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public GraphQLUploadError(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = statusCode;
    }
}
