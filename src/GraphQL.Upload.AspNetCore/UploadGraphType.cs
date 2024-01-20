using GraphQL.Types;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Upload.AspNetCore;

public class UploadGraphType : ScalarGraphType
{
    public UploadGraphType()
    {
        Name = "Upload";
        Description = "A meta type that represents a file upload.";
    }

    public override bool CanParseLiteral(GraphQLValue value) => false;

    public override object? ParseLiteral(GraphQLValue value) => ThrowLiteralConversionError(value, "Upload files must be passed through variables.");

    public override bool CanParseValue(object? value) => value is IFormFile || value == null;

    public override object? ParseValue(object? value) => value switch
    {
        IFormFile _ => value,
        null => null,
        _ => ThrowValueConversionError(value)
    };
}
