namespace AnimalClassifier.Core.Contracts
{
    using Microsoft.AspNetCore.Http;
    public interface IFileValidator
    {
        void ValidateImage(IFormFile file);
        void ValidateVideo(IFormFile file);
        bool IsImage(string path);
    }
}
