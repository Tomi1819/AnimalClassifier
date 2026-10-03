namespace AnimalClassifier.Core.Recognitions
{
    /// <summary>
    /// The kinds of file a recognition can be made from. A recognition keeps
    /// only its file's path, so the extension is what tells an image from a
    /// video wherever one is read back.
    /// </summary>
    public static class MediaFile
    {
        public static readonly IReadOnlySet<string> ImageExtensions =
            new HashSet<string>([".jpg", ".jpeg", ".png"], StringComparer.OrdinalIgnoreCase);

        public static readonly IReadOnlySet<string> VideoExtensions =
            new HashSet<string>([".mp4", ".mov", ".avi"], StringComparer.OrdinalIgnoreCase);

        public static bool IsImage(string path) => ImageExtensions.Contains(Path.GetExtension(path));
    }
}
