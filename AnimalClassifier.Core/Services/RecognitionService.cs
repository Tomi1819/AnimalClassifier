namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using Microsoft.Extensions.ML;
    using OpenCvSharp;
    using System;

    public class RecognitionService : IRecognitionService
    {
        private readonly PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool;

        public RecognitionService(PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool)
        {
            this.predictionEnginePool = predictionEnginePool;
        }
        public async Task<(string PredictedAnimal, float PredictionScore)> PredictAnimalFromImageAsync(string imagePath)
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

        public async Task<List<FramePredictionResult>> PredictAnimalsFromVideoAsync(string videoPath)
        {
            var results = new List<FramePredictionResult>();
            using var capture = new VideoCapture(videoPath);
            int frameRate = (int)capture.Fps;

            for (int i = 0; i < capture.FrameCount; i += frameRate)
            {
                capture.PosFrames = i;
                using var frame = new Mat();
                capture.Read(frame);

                if (frame.Empty()) continue;

                byte[] frameBytes = frame.ToBytes(".jpg");
                var input = new ImageData { ImageSource = frameBytes };
                var prediction = await Task.Run(() => predictionEnginePool.Predict(input));

                results.Add(new FramePredictionResult
                {
                    PredictedAnimal = prediction.PredictedLabel,
                    PredictionScore = prediction.Score.Max(),
                    Timestamp = TimeSpan.FromSeconds(i / frameRate)
                });

            }
            return results;
        }
    }
}
