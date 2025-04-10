namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using Microsoft.Extensions.ML;

    public class RecognitionService : IRecognitionService
    {
        private readonly PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool;

        public RecognitionService(PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool)
        {
            this.predictionEnginePool = predictionEnginePool;
        }
        public async Task<(string PredictedAnimal, float PredictionScore)> PredictAnimalAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image not found.");
            }

            await using var stream = File.OpenRead(imagePath);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var inputData = new ImageData { ImageSource = imageBytes };

            var prediction = predictionEnginePool.Predict(inputData);

            var predictionScore = prediction.Score.Max();

            return (prediction.PredictedLabel, predictionScore);
        }
    }
}
