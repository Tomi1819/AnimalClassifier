namespace AnimalClassifier.Core.Recognitions.Classification.Models
{
    /// <summary>
    /// The animal the model sees in an image, and how sure it is of it.
    /// </summary>
    public class Prediction
    {
        public string Animal { get; set; } = string.Empty;

        /// <summary>
        /// From 0 to 1.
        /// </summary>
        public float Score { get; set; }
    }
}
