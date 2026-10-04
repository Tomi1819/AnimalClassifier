namespace AnimalClassifier.Core.Recognitions.Feedback.Models
{
    using AnimalClassifier.Core.Recognitions.Media;

    /// <summary>
    /// One of a user's recognitions they gave feedback on, and what they said.
    /// </summary>
    public class FeedbackItem
    {
        public int RecognitionId { get; set; }

        /// <summary>
        /// A link the image is loaded by, which runs out after an hour; see
        /// <see cref="IMediaLinkService"/>.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        public string RecognizedAnimal { get; set; } = string.Empty;

        public float PredictionScore { get; set; }

        public DateTime DateRecognized { get; set; }

        public FeedbackDetails Feedback { get; set; } = null!;
    }
}
