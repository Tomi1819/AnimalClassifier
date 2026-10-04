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
        /// The name it was stored under, in its user's folder.
        /// </summary>
        public string FileName { get; set; } = string.Empty;
    }
}
