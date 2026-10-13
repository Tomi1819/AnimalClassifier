namespace AnimalClassifier.Core.Recognitions
{
    /// <summary>
    /// The kinds of file a recognition can be made from. A recognition keeps
    /// only its file's path, so the extension is what tells an image from a
    /// video wherever one is read back.
    /// </summary>
    public static class MediaFile
    {
        /// <summary>
        /// The extension every image is stored with, as each is encoded afresh
        /// as a JPEG whatever it was uploaded as. What is classified, shown and
        /// exported for training is then always the one format.
        /// </summary>
        public const string StoredImageExtension = ".jpg";

        /// <summary>
        /// The extensions a stored image can have: <see cref="StoredImageExtension"/>,
        /// and those of the images stored as they were uploaded, before that.
        /// </summary>
        public static readonly IReadOnlySet<string> ImageExtensions =
            new HashSet<string>([".jpg", ".jpeg", ".png"], StringComparer.OrdinalIgnoreCase);

        public static readonly IReadOnlySet<string> VideoExtensions =
            new HashSet<string>([".mp4", ".mov", ".avi"], StringComparer.OrdinalIgnoreCase);

        public static bool IsImage(string path) => ImageExtensions.Contains(Path.GetExtension(path));
    }
}
