namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;

    public class UploadService : IUploadService
    {
        private readonly IFileValidator fileValidator;
        private readonly IFileStorageService fileStorageService;
        private readonly IRecognitionService recognitionService;
        private readonly IRepository repository;

        public UploadService(
            IFileValidator fileValidator,
            IFileStorageService fileStorageService,
            IRecognitionService recognitionService,
            IRepository repository)
        {
            this.fileValidator = fileValidator;
            this.fileStorageService = fileStorageService;
            this.recognitionService = recognitionService;
            this.repository = repository;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile formFile, string userId)
        {
            fileValidator.Validate(formFile);

            var publicPath = await fileStorageService.SaveFileAsync(formFile, userId);

            var (predictedAnimal, predictionScore) = await recognitionService.PredictAnimalAsync(publicPath);

            var log = new AnimalRecognitionLog
            {
                ImagePath = publicPath,
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
                DateRecognized = log.DateRecognized,
                PredictionScore = predictionScore
            };
        }

        public async Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id)
        {
            return await repository.GetRecognitionLogByIdAsync(id);
        }
    }
}
