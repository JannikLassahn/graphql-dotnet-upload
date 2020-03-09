using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Upload.AspNetCore
{
    /// <summary>
    /// Options for <see cref="GraphQLUploadMiddleware{TSchema}"/>.
    /// </summary>
    public class GraphQLUploadOptions
    {
        /// <summary>
        /// The maximum allowed file size in bytes. Null indicates no limit at all.
        /// </summary>
        public long? MaximumFileSize { get; set; }

        /// <summary>
        /// The maximum allowed amount of files. Null indicates no limit at all.
        /// </summary>
        public long? MaximumFileCount { get; set; }

        /// <summary>
        /// Gets or sets the user context factory.
        /// </summary>
        public Func<HttpContext, IDictionary<string, object>> UserContextFactory { get; set; }
    }
}
