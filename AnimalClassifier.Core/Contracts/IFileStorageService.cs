namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    using Microsoft.AspNetCore.Http;

    public interface IFileStorageService
    {
        Task<StoredFileResult> SaveFileAsync(IFormFile file, string userId);
    }
}
