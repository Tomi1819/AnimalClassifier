namespace AnimalClassifier.Core.Common.Storage
{
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

        public async Task<StoredFile> SaveAsync(string userId, Stream content, string extension)
        {
            var userDirectory = GetUserDirectory(userId);
            Directory.CreateDirectory(userDirectory);

            // Named afresh rather than as uploaded, so that one upload can
            // never overwrite another or name a path of its own choosing.
            var fileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
            var physicalPath = Path.Combine(userDirectory, fileName);

            await using (var file = new FileStream(physicalPath, FileMode.CreateNew))
            {
                await content.CopyToAsync(file);
            }

            return new StoredFile
            {
                PhysicalPath = physicalPath,
                PublicPath = $"{requestPath}/{userId}/{fileName}"
            };
        }

        public void Delete(StoredFile file) => File.Delete(file.PhysicalPath);

        public IEnumerable<string> GetUserFiles(string userId)
        {
            var userDirectory = GetUserDirectory(userId);

            return Directory.Exists(userDirectory)
                ? Directory.GetFiles(userDirectory)
                : [];
        }

        public void DeleteUserFiles(string userId)
        {
            var userDirectory = GetUserDirectory(userId);

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
