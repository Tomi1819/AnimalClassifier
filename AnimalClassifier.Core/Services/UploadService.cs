namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using Microsoft.AspNetCore.Http;
    using System.Globalization;

    public class UploadService : IUploadService
    {
        private readonly IFileValidator fileValidator;
        private readonly IFileStorageService fileStorageService;
        private readonly IRecognitionService recognitionService;
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IUnitOfWork unitOfWork;

        public UploadService(
            IFileValidator fileValidator,
            IFileStorageService fileStorageService,
            IRecognitionService recognitionService,
            IRecognitionLogRepository recognitionLogs,
            IUnitOfWork unitOfWork)
        {
            this.fileValidator = fileValidator;
            this.fileStorageService = fileStorageService;
            this.recognitionService = recognitionService;
            this.recognitionLogs = recognitionLogs;
            this.unitOfWork = unitOfWork;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile formFile, string userId)
        {
            fileValidator.ValidateImage(formFile);

            var storedFile = await SaveAsync(formFile, userId);

            var (predictedAnimal, predictionScore) = await recognitionService.PredictAnimalFromImageAsync(storedFile.PhysicalPath);

            var log = new AnimalRecognitionLog
            {
                ImagePath = storedFile.PublicPath,
                AnimalName = predictedAnimal,
                DateRecognized = DateTime.UtcNow,
                UserId = userId,
                PredictionScore = predictionScore
            };

            recognitionLogs.Add(log);
            await unitOfWork.SaveChangesAsync();

            return new ImageUploadResult
            {
                ImageId = log.Id,
                ImagePath = log.ImagePath,
                RecognizedAnimal = log.AnimalName,
                DateRecognized = log.DateRecognized,
                PredictionScore = predictionScore
            };
        }
        public async Task<VideoUploadResult> UploadVideoAsync(IFormFile formFile, string userId)
        {
            fileValidator.ValidateVideo(formFile);

            var storedFile = await SaveAsync(formFile, userId);

            var recognitionResults = await recognitionService.PredictAnimalsFromVideoAsync(storedFile.PhysicalPath);

            var topAnimals = recognitionResults
                .Where(r => r.PredictionScore >= 0.6f)
                .GroupBy(r => r.PredictedAnimal)
                .Where(g => g.Count() >= 3)
                .Select(g => new AnimalSummary
                {
                    Animal = g.Key,
                    AverageScore = g.Average(x => x.PredictionScore)
                        .ToString("0.00", CultureInfo.InvariantCulture)
                })
                .OrderByDescending(a => a.AverageScore)
                .ToList();

            var topAnimal = topAnimals.FirstOrDefault();

            var log = new AnimalRecognitionLog
            {
                ImagePath = storedFile.PublicPath,
                AnimalName = topAnimal?.Animal ?? UnrecognisedAnimal,
                DateRecognized = DateTime.UtcNow,
                UserId = userId,
                PredictionScore = ParseScore(topAnimal?.AverageScore),
                FramesProcessed = recognitionResults.Count
            };

            recognitionLogs.Add(log);
            await unitOfWork.SaveChangesAsync();

            return new VideoUploadResult
            {
                TopAnimals = topAnimals,
                FramesProcessed = recognitionResults.Count,
                VideoPath = storedFile.PublicPath,
            };
        }
        public Task<AnimalRecognitionLog?> GetRecognitionLogByIdAsync(int id) =>
            recognitionLogs.GetByIdAsync(id);

        public async Task<IEnumerable<RecognitionHistoryItem>> GetHistoryAsync(string userId)
        {
            var logs = await recognitionLogs.GetHistoryAsync(userId);

            return logs.Select(log => new RecognitionHistoryItem
            {
                Id = log.Id,
                MediaPath = log.ImagePath,
                RecognizedAnimal = log.AnimalName,
                DateRecognized = log.DateRecognized,
                PredictionScore = log.PredictionScore,
                FramesProcessed = log.FramesProcessed,
                // A video is stored in the same column as an image, so the path
                // is what tells the two apart.
                IsVideo = !fileValidator.IsImage(log.ImagePath)
            });
        }

        public async Task<int> ClearHistoryAsync(string userId)
        {
            return await recognitionLogs.ClearHistoryAsync(userId);
        }

        private const string UnrecognisedAnimal = "Unknown";

        private async Task<StoredFile> SaveAsync(IFormFile file, string userId)
        {
            await using var content = file.OpenReadStream();

            return await fileStorageService.SaveAsync(userId, content, Path.GetExtension(file.FileName));
        }

        private static float ParseScore(string? averageScore)
        {
            return float.TryParse(
                averageScore,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var score)
                    ? score
                    : 0f;
        }
    }
}
