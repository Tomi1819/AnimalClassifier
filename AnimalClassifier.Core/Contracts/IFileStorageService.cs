namespace AnimalClassifier.Core.Contracts
{
    using Microsoft.AspNetCore.Http;
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string userId);
    }
}
