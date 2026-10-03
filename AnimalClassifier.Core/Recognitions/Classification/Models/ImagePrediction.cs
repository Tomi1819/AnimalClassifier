namespace AnimalClassifier.Core.Recognitions.Classification.Models
{
    using Microsoft.ML.Data;

    /// <summary>
    /// The columns read back from the model. It has others, such as a copy of
    /// the image it was given, which are left out so that nothing copies them.
    /// </summary>
    public class ImagePrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        /// <summary>
        /// The model's confidence in each animal it knows, the predicted one's
        /// the highest.
        /// </summary>
        [ColumnName("Score")]
        public float[] Score { get; set; } = [];
    }
}
