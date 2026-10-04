namespace AnimalClassifier.Core.Recognitions.Uploads.Models
{
    using AnimalClassifier.Core.Recognitions.Media;

    /// <summary>
    /// The recognition an uploaded image was given.
    /// </summary>
    public class ImageUploadResult
    {
        /// <summary>
        /// The recognition's id, which <c>GET api/upload/{id}</c> reads it back by.
        /// </summary>
        public int ImageId { get; set; }

        /// <summary>
        /// A link the image is loaded by, which runs out after an hour; see
        /// <see cref="IMediaLinkService"/>.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        public string RecognizedAnimal { get; set; } = string.Empty;

        public DateTime DateRecognized { get; set; }

        public float PredictionScore { get; set; }
    }
}
