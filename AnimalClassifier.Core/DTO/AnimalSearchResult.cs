namespace AnimalClassifier.Core.DTO
{
    public class AnimalSearchResult
    {
        public string AnimalName { get; set; } = string.Empty;
        public int Count { get; set; }
        public float Accuracy { get; set; }
        public List<string> ImagePaths { get; set; } = new();
    }
}
