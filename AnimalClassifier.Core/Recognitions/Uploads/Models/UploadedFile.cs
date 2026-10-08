namespace AnimalClassifier.Core.Recognitions.Uploads.Models
{
    /// <summary>
    /// A file as an upload brought it in, before anything is made of it. Its
    /// name and content type are whatever the caller said they are; see
    /// <see cref="UploadValidator"/>.
    /// </summary>
    public class UploadedFile
    {
        public required string FileName { get; init; }

        public required string ContentType { get; init; }

        /// <summary>
        /// Its size, in bytes.
        /// </summary>
        public required long Length { get; init; }

        /// <summary>
        /// Opens the file from its start, afresh each time, so that its first
        /// bytes can be checked before the whole of it is read.
        /// </summary>
        public required Func<Stream> OpenReadStream { get; init; }
    }
}
