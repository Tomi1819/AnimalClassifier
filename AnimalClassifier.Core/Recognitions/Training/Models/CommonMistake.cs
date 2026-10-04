namespace AnimalClassifier.Core.Recognitions.Training.Models
{
    /// <summary>
    /// An animal the model named in place of another it knows, and how often
    /// users said so.
    /// </summary>
    public class CommonMistake
    {
        public string RecognizedAnimal { get; set; } = string.Empty;

        public string ActualAnimal { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
