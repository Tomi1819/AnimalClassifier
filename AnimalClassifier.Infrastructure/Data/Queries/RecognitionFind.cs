namespace AnimalClassifier.Infrastructure.Data.Queries
{
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
    }
}
