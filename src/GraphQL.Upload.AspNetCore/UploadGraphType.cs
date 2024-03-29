﻿using GraphQL.Types;
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

        public override object ParseValue(object value) => value switch
        {
            IFormFile _ => value,
            null => null,
            _ => ThrowValueConversionError(value)
        };
    }
}
