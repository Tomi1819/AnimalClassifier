namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.ML;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;

    public class ModelService : IModelService
    {
        private readonly MLContext mLContext;
        private ITransformer model;
        private readonly PredictionEngine<ModelInput, ModelOutput> predictionEngine;
        private readonly string modelPath;
        private readonly IRepository repository;

        public ModelService(IConfiguration configuration, IRepository repository)
        {
            this.mLContext = new MLContext();
            this.modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModels", "MLModel.mlnet");

            try
            {
                this.model = mLContext.Model.Load(modelPath, out var modelInputSchema);
                predictionEngine = mLContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(this.model);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error loading the model.", ex);
            }

            this.repository = repository;
        }

        public async Task<AnimalRecognitionLog> PredictAnimalAsync(string imagePath, string userId)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ArgumentException("Image path cannot be null or empty.");
            }

            byte[] imageBytes;

            try
            {
                imageBytes = await File.ReadAllBytesAsync(imagePath);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Error reading the image file.", ex);
            }

            string predictedLabel = PredictLabel(imageBytes);

            var recognitionLog = new AnimalRecognitionLog()
            {
                AnimalName = predictedLabel,
                ImagePath = imagePath,
                DateRecognized = DateTime.UtcNow,
                UserId = userId
            };

            await repository.AddRecognitionLogAsync(recognitionLog);
            await repository.SaveChangesAsync();

            return recognitionLog;
        }

        private string PredictLabel(byte[] imageBytes)
        {
            var imageData = new ModelInput()
            {
                Image = LoadImage(imageBytes)
            };

            if (imageData.Image == null)
            {
                throw new ArgumentException("Failed to load image.");
            }

            try
            {
                var prediction = predictionEngine.Predict(imageData);
                return prediction.PredictedLabel;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error during prediction.", ex);
            }
        }

        private Bitmap LoadImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Image data cannot be null or empty.");
            }

            try
            {
                using (var stream = new MemoryStream(imageBytes))
                {
                    return new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error loading image.", ex);
            }
        }
    }
}
