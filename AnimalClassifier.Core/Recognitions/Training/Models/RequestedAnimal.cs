namespace AnimalClassifier.Core.Recognitions.Training.Models
{
    /// <summary>
    /// An animal the model does not know, and how often users named it.
    /// </summary>
    public class RequestedAnimal
    {
        public string Animal { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
