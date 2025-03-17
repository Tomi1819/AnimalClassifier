namespace AnimalClassifier.Core.DTO
{
    using Microsoft.ML.Data;

    public class ImageData
    {
        [LoadColumn(0)]
        [ColumnName("Label")]
        public string Label { get; set; } = string.Empty;

        [LoadColumn(1)]
        [ColumnName("ImageSource")]
        public byte[] ImageSource { get; set; } = Array.Empty<byte>();
    }
}

