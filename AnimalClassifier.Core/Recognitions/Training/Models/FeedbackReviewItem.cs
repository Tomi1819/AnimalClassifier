namespace AnimalClassifier.Core.Recognitions.Training.Models
{
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Infrastructure.Data.Models;
    using System.Text.Json.Serialization;

    /// <summary>
    /// One feedback offered for training, as an administrator reviews it. It
    /// leaves out whose it is, which deciding whether its image shows the
    /// animal it says does not need.
    /// </summary>
    public class FeedbackReviewItem
    {
        public int Id { get; set; }

        /// <summary>
        /// A link the image is loaded by, which runs out after an hour; see
        /// <see cref="IMediaLinkService"/>.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        public string RecognizedAnimal { get; set; } = string.Empty;

        public float PredictionScore { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<FeedbackVerdict>))]
        public FeedbackVerdict Verdict { get; set; }

        /// <summary>
        /// The animal the image would be trained as.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Whether the model knows <see cref="Label"/> already. One it does
        /// not would be a new animal for it to learn.
        /// </summary>
        public bool IsKnownAnimal { get; set; }

        public string? Comment { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<FeedbackReviewStatus>))]
        public FeedbackReviewStatus ReviewStatus { get; set; }

        public DateTime DateSubmitted { get; set; }

        public DateTime? DateReviewed { get; set; }
    }
}
