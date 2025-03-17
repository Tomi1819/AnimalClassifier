namespace AnimalClassifier.Core.DTO
{
    using Microsoft.ML.Data;
    public class ImagePrediction
    {
        [ColumnName("PredictedLabelId")]
        public uint PredictedLabelId { get; set; }

        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        [ColumnName("ImageSource")]
        public byte[] ImageSource { get; set; } = Array.Empty<byte>();

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }

}
