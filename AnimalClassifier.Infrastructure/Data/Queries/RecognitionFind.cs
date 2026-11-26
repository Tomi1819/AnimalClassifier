namespace AnimalClassifier.Infrastructure.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// A recognition with the animal it finds; see
    /// <see cref="RecognitionFinds"/>.
    /// </summary>
    internal sealed class RecognitionFind
    {
        public int Id { get; init; }
        public string FileName { get; init; } = string.Empty;
        public DateTime DateRecognized { get; init; }
        public float PredictionScore { get; init; }

        /// <summary>
        /// The animal the model named.
        /// </summary>
        public string RecognizedAnimal { get; init; } = string.Empty;

        public bool HasFeedback { get; init; }

        /// <summary>
        /// The animal it finds, as the recognition or its feedback names it;
        /// null when it finds none.
        /// </summary>
        public string? AnimalName { get; init; }

        /// <summary>
        /// What the user said of it; null when they said nothing.
        /// </summary>
        public FeedbackVerdict? Verdict { get; init; }

        /// <summary>
        /// When they said it, or last changed it.
        /// </summary>
        public DateTime? DateFeedbackSubmitted { get; init; }

        /// <summary>
        /// When an administrator accepted it to train the model on; null
        /// unless the user allowed that and it was accepted.
        /// </summary>
        public DateTime? DateAcceptedForTraining { get; init; }
    }
}
