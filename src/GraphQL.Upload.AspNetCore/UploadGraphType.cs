using System;
using GraphQL.Language.AST;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Upload.AspNetCore
{
    public class UploadGraphType : ScalarGraphType
    {
        public UploadGraphType()
        {
            Name = "Upload";
            Description = "A meta type that represents a file upload.";
        }

        public override object ParseLiteral(IValue value)
        {
            if (value is NullValue)
            {
                return null;
            }

            if (value is FormFileValue)
            {
                return value.Value;
            }

            return ThrowLiteralConversionError(value);
        }

        public override object ParseValue(object value)
        {
            return value;
        }

        /// <inheritdoc />
        public override IValue ToAST(object value)
        {
            var serialized = Serialize(value);
            if (serialized is IFormFile formFile)
            {
                return new FormFileValue(formFile);
            }

            if (serialized is null)
            {
                return new NullValue();
            }

            throw new NotImplementedException($"Please override the '{nameof(ToAST)}' method of the '{GetType().Name}' scalar to support this operation.");
        }
    }

    internal class FormFileValue : ValueNode<IFormFile>
    {
        public FormFileValue(IFormFile value)
        {
            Value = value;
        }
    }
}
