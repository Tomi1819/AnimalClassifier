namespace AnimalClassifier.Core.Contracts
{
    using Microsoft.AspNetCore.Http;
    public interface IFileValidator
    {
        void Validate(IFormFile file);
    }
}
