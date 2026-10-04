namespace AnimalClassifier.Core.Recognitions.Uploads
{
    /// <summary>
    /// What uploading tells the user. Each is written to be shown as it
    /// stands.
    /// </summary>
    public static class UploadMessages
    {
        public const string EmptyFile = "The file is empty.";

        /// <summary>
        /// Formatted with the largest size allowed, in megabytes.
        /// </summary>
        public const string FileTooLarge = "The file is too large. It can be at most {0} MB.";

        public const string UnsupportedImage = "Only JPEG and PNG images can be uploaded.";
        public const string UnreadableImage = "The image could not be read. Please try another one.";

        /// <summary>
        /// Formatted with the most pixels allowed, in millions.
        /// </summary>
        public const string ImageTooLarge = "The image is too large. It can be at most {0} megapixels.";

        public const string UnsupportedVideo = "Only MP4, MOV and AVI videos can be uploaded.";
        public const string RecognitionNotFound = "The recognition does not exist.";
    }
}
