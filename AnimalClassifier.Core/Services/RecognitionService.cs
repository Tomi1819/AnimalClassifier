namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using Microsoft.ML;

    public class RecognitionService : IRecognitionService
    {
        private static readonly string modelPath = "C:\\UNI\\Practice\\AnimalClassifier\\AnimalClassifier\\MLModel.mlnet";
        private readonly PredictionEngine<ImageData, ImagePrediction> predictionEngine;

        public RecognitionService()
        {
            var mlContext = new MLContext();
            var trainedModel = mlContext.Model.Load(modelPath, out var modelInputSchema);
            this.predictionEngine = mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(trainedModel);
        }

        public async Task<string> PredictAnimalAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image not found.");
            }

            var imageBytes = await File.ReadAllBytesAsync(imagePath);

            var inputData = new ImageData { ImageSource = imageBytes };

            var prediction = await Task.Run(() => predictionEngine.Predict(inputData));

            return prediction.PredictedLabel;
        }
    }
}

