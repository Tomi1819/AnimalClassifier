namespace AnimalClassifier.Core.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// What one recognition counts towards a user's achievements: what the
    /// model saw, what it found by the collection's rule, and what the user
    /// said of it. See <see cref="IRecognitionAchievementQueries"/>.
    /// </summary>
    public sealed record RecognitionFacts
    {
        public int Id { get; init; }

        /// <summary>
        /// The name of the image or video it was made from, whose extension
        /// tells the two apart.
        /// </summary>
        public string FileName { get; init; } = string.Empty;

        public DateTime DateRecognized { get; init; }

        /// <summary>
        /// The animal the model named, as the recognition records it.
        /// </summary>
        public string RecognizedAnimal { get; init; } = string.Empty;

        public float PredictionScore { get; init; }

        /// <summary>
        /// The animal it finds, by the rule <see cref="IRecognitionCollectionQueries"/>
        /// sets out; null when it finds none.
        /// </summary>
        public string? FoundAnimal { get; init; }

        /// <summary>
        /// What the user said of it; null when they said nothing.
        /// </summary>
        public FeedbackVerdict? Verdict { get; init; }

        /// <summary>
        /// When they said it, or last changed it; null when they said nothing.
        /// </summary>
        public DateTime? DateFeedbackSubmitted { get; init; }

        /// <summary>
        /// When an administrator accepted its image to train the model on;
        /// null unless the user allowed that and it was accepted.
        /// </summary>
        public DateTime? DateAcceptedForTraining { get; init; }
    }
}
