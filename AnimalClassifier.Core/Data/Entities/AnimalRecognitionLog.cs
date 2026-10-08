namespace AnimalClassifier.Core.Data.Entities
{
    public class AnimalRecognitionLog
    {
        public int Id { get; set; }
        public string AnimalName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the image or video it was made from, among the files
        /// its user uploaded.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        public DateTime DateRecognized { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// The model's confidence in <see cref="AnimalName"/>. For a video this
        /// is the average score of its strongest animal.
        /// </summary>
        public float PredictionScore { get; set; }

        /// <summary>
        /// The number of frames examined, which only a video has.
        /// </summary>
        public int? FramesProcessed { get; set; }

        /// <summary>
        /// Set when the user clears their history. The recognition still
        /// happened, so the statistics keep counting it; the owner's history
        /// and the search leave it out.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// What its user said of it, if they said anything.
        /// </summary>
        public RecognitionFeedback? Feedback { get; set; }
    }
}
