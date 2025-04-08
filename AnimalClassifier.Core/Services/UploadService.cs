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
        private readonly IRecognitionService recognitionService;
        private readonly string uploadRootPath;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public UploadService(
            IRepository repository,
            IRecognitionService recognitionService,
            IOptions<UploadSettings> options)
        {
            this.repository = repository;
            this.recognitionService = recognitionService;
            uploadRootPath = options.Value.UploadPath;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile formFile, string userId)
        {
            ValidateFile(formFile);

            string userDirectory = Path.Combine(uploadRootPath, userId);
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }

            string extension = Path.GetExtension(formFile.FileName).ToLower();
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string fullFilePath = Path.Combine(userDirectory, uniqueFileName);

            await using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            string predictedAnimal = await recognitionService.PredictAnimalAsync(fullFilePath);

            string publicImagePath = $"/uploads/{userId}/{uniqueFileName}";

            var log = new AnimalRecognitionLog
            {
                ImagePath = publicImagePath,
                AnimalName = predictedAnimal,
                DateRecognized = DateTime.UtcNow,
                UserId = userId
            };

            await repository.AddRecognitionLogAsync(log);
            await repository.SaveChangesAsync();

            return new ImageUploadResult
            {
                ImageId = log.Id,
                ImagePath = log.ImagePath,
                RecognizedAnimal = log.AnimalName,
                DateRecognized = log.DateRecognized
            };
        }

        public async Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id)
        {
            return await repository.GetRecognitionLogByIdAsync(id);
        }

        private void ValidateFile(IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0)
                throw new ArgumentException("The file is empty.");

            if (formFile.Length > MaxFileSize)
                throw new ArgumentException($"The file is too large. Max size: {MaxFileSize / (1024 * 1024)} MB.");

            string extension = Path.GetExtension(formFile.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("Unsupported file extension.");

            string mime = formFile.ContentType.ToLower();
            if (!Regex.IsMatch(mime, @"^image\/(jpeg|png)$"))
                throw new ArgumentException("Unsupported MIME type.");
        }
    }
}
