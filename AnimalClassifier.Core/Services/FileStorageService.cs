namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public class FileStorageService : IFileStorageService
    {
        private readonly string uploadRootPath;
        private readonly string requestPath;

        public FileStorageService(IOptions<UploadSettings> options)
        {
            uploadRootPath = options.Value.UploadPath;
            requestPath = options.Value.RequestPath;
        }

        public async Task<StoredFileResult> SaveFileAsync(IFormFile file, string userId)
        {
            string userDirectory = Path.Combine(uploadRootPath, userId);
            Directory.CreateDirectory(userDirectory);

            string extension = Path.GetExtension(file.FileName).ToLower();
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string physicalPath = Path.Combine(userDirectory, uniqueFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new StoredFileResult
            {
                PhysicalPath = physicalPath,
                PublicPath = $"{requestPath}/{userId}/{uniqueFileName}"
            };
        }
    }
}
