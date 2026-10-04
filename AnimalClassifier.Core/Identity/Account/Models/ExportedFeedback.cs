namespace AnimalClassifier.Core.Identity.Account.Models
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using System.Text.Json.Serialization;

    /// <summary>
    /// What the owner said of one of their recognitions, in their copy of
    /// their data.
    /// </summary>
    public class ExportedFeedback
    {
        [JsonConverter(typeof(JsonStringEnumConverter<FeedbackVerdict>))]
        public FeedbackVerdict Verdict { get; set; }

        /// <summary>
        /// The animal the image shows; null when the model was right.
        /// </summary>
        public string? ActualAnimal { get; set; }

        public string? Comment { get; set; }

        public bool AllowsTraining { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<FeedbackReviewStatus>))]
        public FeedbackReviewStatus ReviewStatus { get; set; }

        public DateTime DateSubmitted { get; set; }
    }
}
