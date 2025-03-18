namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using System.Text.RegularExpressions;

    public class UploadService : IUploadService
    {
        private readonly IRepository repository;
        private readonly string uploadPath;
        private readonly IRecognitionService recognitionService;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        private const long MaxFileSize = 5 * 1024 * 1024;

        public UploadService(IRepository repository, IOptions<UploadSettings> uploadSettings, IRecognitionService recognitionService)
        {
            this.repository = repository;
            this.recognitionService = recognitionService;
            this.uploadPath = Path.Join(uploadSettings.Value.UploadPath ?? "wwwroot/uploads");
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile formFile, string userId)
        {
            ValidateFile(formFile);

            string userFolderPath = await PrepareUserUploadDirectoryAsync(userId);
            string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(formFile.FileName).ToLower()}";
            string filePath = Path.Join(userFolderPath, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            string predictedLabel = await recognitionService.PredictAnimalAsync(filePath);

            var imagePath = $"/uploads/{userId}/{uniqueFileName}";

            var animalRecognitionLog = new AnimalRecognitionLog
            {
                ImagePath = imagePath,
                AnimalName = predictedLabel,
                DateRecognized = DateTime.UtcNow,
                UserId = userId
            };

            await repository.AddRecognitionLogAsync(animalRecognitionLog);
            await repository.SaveChangesAsync();

            return new ImageUploadResult
            {
                ImageId = animalRecognitionLog.Id,
                ImagePath = animalRecognitionLog.ImagePath,
                RecognizedAnimal = predictedLabel,
                DateRecognized = animalRecognitionLog.DateRecognized
            };
        }

        public async Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id)
        {
            return await repository.GetRecognitionLogByIdAsync(id);
        }

        private async Task<string> PrepareUserUploadDirectoryAsync(string userId)
        {
            string userFolderPath = Path.Join(uploadPath, userId);

            if (!Directory.Exists(userFolderPath))
            {
                await Task.Run(() => Directory.CreateDirectory(userFolderPath));
            }

            return userFolderPath;
        }

        private void ValidateFile(IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0)
            {
                throw new ArgumentException("The file is empty.");
            }

            if (formFile.Length > MaxFileSize)
            {
                throw new ArgumentException($"The file size is too large. Maximum allowed size is {MaxFileSize / 1024 / 1024} MB.");
            }

            string extension = Path.GetExtension(formFile.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file extension. Only .jpg, .jpeg, and .png are allowed.");
            }

            var mimeType = formFile.ContentType;
            if (!Regex.IsMatch(mimeType, @"^image\/(jpeg|png)$", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException("Invalid MIME type. Only image/jpeg and image/png are allowed.");
            }
        }
    }
}
