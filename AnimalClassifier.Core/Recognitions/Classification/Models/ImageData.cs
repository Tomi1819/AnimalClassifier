namespace AnimalClassifier.Core.Recognitions.Classification.Models
{
    using Microsoft.ML.Data;

    /// <summary>
    /// What the model reads, in the columns it was trained with. The label is
    /// only filled in for training, but the model's pipeline still expects
    /// the column.
    /// </summary>
    public class ImageData
    {
        [LoadColumn(0)]
        [ColumnName("Label")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The encoded image, such as a JPEG or PNG file's bytes.
        /// </summary>
        [LoadColumn(1)]
        [ColumnName("ImageSource")]
        public byte[] ImageSource { get; set; } = [];
    }
}
