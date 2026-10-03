namespace AnimalClassifier.Core.Common.Storage
{
    /// <summary>
    /// An uploaded file, once it has been stored.
    /// </summary>
    public class StoredFile
    {
        /// <summary>
        /// Where the file is on disk, for the server to read.
        /// </summary>
        public string PhysicalPath { get; set; } = string.Empty;

        /// <summary>
        /// Where the file is served from, for a browser to load.
        /// </summary>
        public string PublicPath { get; set; } = string.Empty;
    }
}
