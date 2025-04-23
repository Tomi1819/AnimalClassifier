namespace AnimalClassifier.Core.DTO
{
    public class FramePredictionResult
    {
        public string FramePath { get; set; } = string.Empty;
        public string PredictedAnimal { get; set; } = string.Empty;
        public float PredictionScore { get; set; }
        public TimeSpan Timestamp { get; set; }
    }
}
