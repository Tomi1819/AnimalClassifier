namespace AnimalClassifier.Infrastructure.Data.Models
{
    public class AnimalRecognitionLog
    {
        public int Id { get; set; }
        public string AnimalName { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
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
        /// happened, so the statistics and search pages keep counting it; only
        /// the owner's own history hides it.
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
