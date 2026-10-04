namespace AnimalClassifier.Core.Recognitions.History.Models
{
    using AnimalClassifier.Core.Recognitions.Media;

    /// <summary>
    /// One entry in a user's own recognition history.
    /// </summary>
    public class RecognitionHistoryItem
    {
        public int Id { get; set; }

        /// <summary>
        /// A link the uploaded image or video is loaded by, which runs out
        /// after an hour; see <see cref="IMediaLinkService"/>.
        /// </summary>
        public string MediaPath { get; set; } = string.Empty;

        public string RecognizedAnimal { get; set; } = string.Empty;

        public DateTime DateRecognized { get; set; }

        public float PredictionScore { get; set; }

        /// <summary>
        /// The number of frames examined; null for an image.
        /// </summary>
        public int? FramesProcessed { get; set; }

        public bool IsVideo { get; set; }
    }
}
