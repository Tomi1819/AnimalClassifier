namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public class FileStorageService : IFileStorageService
    {
        private readonly string uploadRootPath;

        public FileStorageService(IOptions<UploadSettings> options)
        {
            uploadRootPath = options.Value.UploadPath;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string userId)
        {
            string userDirectory = Path.Combine(uploadRootPath, userId);
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string fullFilePath = Path.Combine(userDirectory, uniqueFileName);

            await using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{userId}/{uniqueFileName}";
        }
    }
}
