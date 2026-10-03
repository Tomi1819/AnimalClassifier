namespace AnimalClassifier.Core.Recognitions.Uploads.Models
{
    /// <summary>
    /// The recognition an uploaded image was given.
    /// </summary>
    public class ImageUploadResult
    {
        /// <summary>
        /// The recognition's id, which <c>GET api/upload/{id}</c> reads it back by.
        /// </summary>
        public int ImageId { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        public string RecognizedAnimal { get; set; } = string.Empty;

        public DateTime DateRecognized { get; set; }

        public float PredictionScore { get; set; }
    }
}
