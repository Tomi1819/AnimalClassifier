namespace AnimalClassifier.Core.Contracts
{
    public interface IRecognitionService
    {
        Task<(string PredictedAnimal, float PredictionScore)> PredictAnimalAsync(string imagePath);
    }
}
