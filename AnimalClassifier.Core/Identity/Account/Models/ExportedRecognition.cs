namespace AnimalClassifier.Core.Identity.Account.Models
{
    /// <summary>
    /// One recognition in its owner's copy of their data.
    /// </summary>
    public class ExportedRecognition
    {
        public string RecognizedAnimal { get; set; } = string.Empty;

        public float PredictionScore { get; set; }

        /// <summary>
        /// The number of frames examined; null for an image.
        /// </summary>
        public int? FramesProcessed { get; set; }

        public DateTime DateRecognized { get; set; }

        /// <summary>
        /// Whether the owner cleared it from their history. It is still kept,
        /// and counted by the statistics, so the copy includes it.
        /// </summary>
        public bool IsCleared { get; set; }

        /// <summary>
        /// The uploaded image or video, as a path inside the archive.
        /// </summary>
        public string File { get; set; } = string.Empty;
    }
}
