namespace AnimalClassifier.Core.Recognitions.Training
{
    /// <summary>
    /// What reviewing feedback tells an administrator. Each is written to be
    /// shown as it stands.
    /// </summary>
    public static class TrainingMessages
    {
        public const string FeedbackNotFound = "The feedback does not exist, or its user did not allow training on it.";
        public const string FeedbackAlreadyAccepted = "The feedback is already accepted.";
        public const string FeedbackAlreadyRejected = "The feedback is already rejected.";
    }
}
