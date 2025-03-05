namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;

    public interface IUploadService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile formFile, string userId);
        Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id);
    }
}
