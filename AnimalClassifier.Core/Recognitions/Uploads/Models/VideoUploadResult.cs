namespace AnimalClassifier.Core.Recognitions.Uploads.Models
{
    using AnimalClassifier.Core.Recognitions.Media;

    /// <summary>
    /// What an uploaded video was found to show.
    /// </summary>
    public class VideoUploadResult
    {
        /// <summary>
        /// How many of its frames were classified.
        /// </summary>
        public int FramesProcessed { get; set; }

        /// <summary>
        /// The animals seen clearly enough to count, strongest first. None for
        /// a video in which no animal was.
        /// </summary>
        public List<AnimalSummary> TopAnimals { get; set; } = [];

        /// <summary>
        /// A link the video is loaded by, which runs out after an hour; see
        /// <see cref="IMediaLinkService"/>.
        /// </summary>
        public string VideoPath { get; set; } = string.Empty;
    }
}
