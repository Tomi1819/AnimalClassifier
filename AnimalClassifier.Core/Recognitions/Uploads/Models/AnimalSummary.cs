namespace AnimalClassifier.Core.Recognitions.Uploads.Models
{
    /// <summary>
    /// One animal seen in a video.
    /// </summary>
    public class AnimalSummary
    {
        public string Animal { get; set; } = string.Empty;

        /// <summary>
        /// The model's average confidence in it over the frames it was seen
        /// in, written with two decimals, such as <c>0.87</c>.
        /// </summary>
        public string AverageScore { get; set; } = string.Empty;
    }
}
