namespace AnimalClassifier.Core.Recognitions.Feedback
{
    /// <summary>
    /// What giving feedback tells the user. Each is written to be shown as it
    /// stands.
    /// </summary>
    public static class FeedbackMessages
    {
        public const string RecognitionNotFound = "The recognition does not exist.";
        public const string FeedbackNotFound = "There is no feedback on this recognition.";
        public const string ImagesOnly = "Feedback can only be given on an image.";
        public const string InvalidVerdict = "Please say whether the model was right.";
        public const string AnimalNotExpected = "An animal is only named when the model was wrong.";
        public const string MissingAnimal = "Please say which animal it was.";
        public const string UnknownAnimal = "The model does not know that animal. Choose one from the list, or say it is not listed.";
        public const string SameAnimal = "That is the animal the model named. Say that it was right instead.";
        public const string KnownAnimal = "The model knows that animal. Choose it from the list instead.";

        /// <summary>
        /// Formatted with the most characters a name can have.
        /// </summary>
        public const string InvalidAnimalName = "An animal's name can only hold letters, with spaces, hyphens or apostrophes between them, and at most {0} characters.";

        /// <summary>
        /// Formatted with the most characters a comment can have.
        /// </summary>
        public const string CommentTooLong = "The comment can be at most {0} characters.";
    }
}
