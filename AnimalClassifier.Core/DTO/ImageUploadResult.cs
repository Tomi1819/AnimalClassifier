namespace AnimalClassifier.Core.DTO
{
    public class ImageUploadResult
    {
        public int ImageId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string RecognizedAnimal { get; set; } = string.Empty;
        public DateTime DateRecognized { get; set; }
        public float PredictionScore { get; set; }
    }
}
