namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;

    public interface IModelService
    {
        Task<AnimalRecognitionLog> PredictAnimalAsync(string imagePath, string userId);
    }
}
