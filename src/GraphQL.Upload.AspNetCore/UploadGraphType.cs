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
            var formFileValue = value as FormFileValue;
            return formFileValue?.Value;
        }

        public override object ParseValue(object value)
        {
            return value;
        }

        public override object Serialize(object value)
        {
            return ParseValue(value);
        }
    }

    public class FormFileValue : ValueNode<IFormFile>
    {
        public FormFileValue(IFormFile value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<IFormFile> node)
        {
            return Value.Equals(node.Value);
        }
    }

    public class FormFileConverter : IAstFromValueConverter
    {
        public IValue Convert(object value, IGraphType type)
        {
            return new FormFileValue(value as IFormFile);
        }

        public bool Matches(object value, IGraphType type)
        {
            return value is IFormFile;
        }
    }
}
