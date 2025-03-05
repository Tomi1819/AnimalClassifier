namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public class UploadService : IUploadService
    {
        private readonly IRepository repository;
        private readonly string uploadPath;

        private const long MaxFileSize = 5 * 1024 * 1024;

        public UploadService(IRepository repository, IOptions<UploadSettings> uploadSettings)
        {
            this.repository = repository;
            this.uploadPath = uploadSettings.Value.UploadPath;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile formFile, string userId)
        {
            if (formFile is null || formFile.Length == 0)
            {
                throw new ArgumentException("The file is empty.");
            }

            if (formFile.Length > MaxFileSize)
            {
                throw new ArgumentException($"The file size is too large. Maximum allowed size is {MaxFileSize / 1024 / 1024} MB.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(formFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file extension. Only .jpg, .jpeg, and .png are allowed.");
            }

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(uploadPath, uniqueFileName);

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            var animalRecognitionLog = new AnimalRecognitionLog()
            {
                ImagePath = $"/uploads/{uniqueFileName}",
                DateRecognized = DateTime.Now,
                UserId = userId
            };

            await repository.AddRecognitionLogAsync(animalRecognitionLog);
            await repository.SaveChangesAsync();

            return new ImageUploadResult
            {
                ImageId = animalRecognitionLog.Id,
                ImagePath = animalRecognitionLog.ImagePath,
                DateRecognized = animalRecognitionLog.DateRecognized
            };
        }
        public async Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id)
        {
            return await repository.GetRecognitionLogByIdAsync(id);
        }
    }
}
