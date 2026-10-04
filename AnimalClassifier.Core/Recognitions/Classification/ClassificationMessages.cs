namespace AnimalClassifier.Core.Recognitions.Classification
{
    /// <summary>
    /// What classifying an upload tells the user. Each is written to be shown
    /// as it stands.
    /// </summary>
    public static class ClassificationMessages
    {
        public const string UnreadableVideo = "The video could not be read. Please try another one.";
        public const string VideoTooLarge = "The video's resolution is too high. It can be at most 4K.";
    }
}
