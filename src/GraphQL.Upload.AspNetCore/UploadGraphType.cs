using GraphQL.Types;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Upload.AspNetCore;

/// <summary>
/// Represents a GraphQL scalar type named 'Upload' for handling file uploads.
/// </summary>
/// <remarks>
/// This scalar type is used to represent file uploads in a GraphQL schema.
/// It is designed to work with multipart form data in GraphQL requests.
/// </remarks>
public class UploadGraphType : ScalarGraphType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadGraphType"/> class.
    /// </summary>
    public UploadGraphType()
    {
        Name = "Upload";
        Description = "A meta type that represents a file upload.";
    }

    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => false;

    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value)
        => ThrowLiteralConversionError(value, "Upload files must be passed through variables.");

    /// <inheritdoc/>
    public override bool CanParseValue(object? value) => value is IFormFile || value == null;

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        IFormFile _ => value,
        null => null,
        _ => ThrowValueConversionError(value)
    };
}
