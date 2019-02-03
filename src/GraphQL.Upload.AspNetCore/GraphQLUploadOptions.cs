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
    }
}
