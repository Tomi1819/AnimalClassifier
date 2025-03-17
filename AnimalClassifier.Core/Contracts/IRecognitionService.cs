namespace AnimalClassifier.Core.Contracts
{
    public interface IRecognitionService
    {
        Task<string> PredictAnimalAsync(string imagePath);
    }
}
