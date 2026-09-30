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
            string userDirectory = GetUserDirectory(userId);
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

        public IEnumerable<string> GetUserFiles(string userId)
        {
            string userDirectory = GetUserDirectory(userId);

            return Directory.Exists(userDirectory)
                ? Directory.GetFiles(userDirectory)
                : [];
        }

        public void DeleteUserFiles(string userId)
        {
            string userDirectory = GetUserDirectory(userId);

            if (Directory.Exists(userDirectory))
            {
                Directory.Delete(userDirectory, recursive: true);
            }
        }

        // An empty id would name the upload root itself, and with it every
        // user's files.
        private string GetUserDirectory(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            return Path.Combine(uploadRootPath, userId);
        }
    }
}
