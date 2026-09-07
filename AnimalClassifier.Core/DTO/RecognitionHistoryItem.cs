namespace AnimalClassifier.Core.DTO
{
    /// <summary>
    /// One entry in a user's own recognition history.
    /// </summary>
    public class RecognitionHistoryItem
    {
        public int Id { get; set; }

        /// <summary>
        /// The public path of the uploaded image or video.
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
