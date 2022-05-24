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

        public override object ParseValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is IFormFile formFile)
            {
                return formFile;
            }

            return ThrowValueConversionError(value);
        }
    }
}
