namespace AnimalClassifier.Core.DTO
{
    public class VideoUploadResult
    {
        public int FramesProcessed { get; set; }
        public List<AnimalSummary> TopAnimals { get; set; } = new();
    }
}
