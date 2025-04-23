using AnimalClassifier.Core.DTO;

namespace AnimalClassifier.Core.Contracts
{
    public interface IRecognitionService
    {
        Task<(string PredictedAnimal, float PredictionScore)> PredictAnimalFromImageAsync(string imagePath);
        Task<List<FramePredictionResult>> PredictAnimalsFromVideoAsync(string videoPath);
    }
}
