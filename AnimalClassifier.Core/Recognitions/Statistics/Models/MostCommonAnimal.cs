namespace AnimalClassifier.Core.Recognitions.Statistics.Models
{
    /// <summary>
    /// One of the animals recognised most, and how many times it was.
    /// </summary>
    public class MostCommonAnimal
    {
        public string AnimalName { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
