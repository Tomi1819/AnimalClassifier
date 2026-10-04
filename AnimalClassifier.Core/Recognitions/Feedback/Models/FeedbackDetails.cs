namespace AnimalClassifier.Core.Recognitions.Feedback.Models
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using System.Text.Json.Serialization;

    /// <summary>
    /// The feedback a user gave on one of their recognitions, as they gave it
    /// and where its review stands.
    /// </summary>
    public class FeedbackDetails
    {
        [JsonConverter(typeof(JsonStringEnumConverter<FeedbackVerdict>))]
        public FeedbackVerdict Verdict { get; set; }

        /// <summary>
        /// The animal the image shows; null when the model was right.
        /// </summary>
        public string? ActualAnimal { get; set; }

        public string? Comment { get; set; }

        public bool AllowsTraining { get; set; }

        /// <summary>
        /// Only feedback that allows training is reviewed, so for any other
        /// this stays <see cref="FeedbackReviewStatus.Pending"/>.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter<FeedbackReviewStatus>))]
        public FeedbackReviewStatus ReviewStatus { get; set; }

        /// <summary>
        /// When it was given, or last changed.
        /// </summary>
        public DateTime DateSubmitted { get; set; }

        public static FeedbackDetails From(RecognitionFeedback feedback) => new()
        {
            Verdict = feedback.Verdict,
            ActualAnimal = feedback.ActualAnimal,
            Comment = feedback.Comment,
            AllowsTraining = feedback.AllowsTraining,
            ReviewStatus = feedback.ReviewStatus,
            DateSubmitted = feedback.DateSubmitted
        };
    }
}
