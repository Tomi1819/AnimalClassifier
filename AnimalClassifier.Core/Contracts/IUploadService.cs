namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;

    public interface IUploadService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile formFile, string userId);
        Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id);

        /// <summary>
        /// One user's own recognitions, most recent first, excluding any they
        /// have cleared.
        /// </summary>
        Task<IEnumerable<RecognitionHistoryItem>> GetHistoryAsync(string userId);

        /// <summary>
        /// Clears one user's history and returns how many entries were cleared.
        /// </summary>
        Task<int> ClearHistoryAsync(string userId);
        Task<VideoUploadResult> UploadVideoAsync(IFormFile formFile, string userId);
    }
}
