namespace AnimalClassifier.Core.Recognitions.Training
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// The animal an image is trained as, by what its user said of it.
    /// </summary>
    public static class TrainingLabel
    {
        /// <returns>
        /// The animal the model named when the user said it was right, and the
        /// one they named instead when it was not.
        /// </returns>
        public static string For(RecognitionFeedback feedback) =>
            feedback.Verdict == FeedbackVerdict.Correct
                ? feedback.Recognition.AnimalName
                : feedback.ActualAnimal!;

        /// <summary>
        /// Whether the model could be trained on it as it is. An animal it
        /// does not know needs a new folder in the dataset, which is a choice
        /// to be made rather than something to merge.
        /// </summary>
        public static bool IsKnownToTheModel(RecognitionFeedback feedback) =>
            feedback.Verdict != FeedbackVerdict.UnlistedAnimal;
    }
}
