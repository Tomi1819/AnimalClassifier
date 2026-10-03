namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Recognitions.Classification.Models;
    using Microsoft.Extensions.ML;

    /// <summary>
    /// The trained model, run through a pool of prediction engines, since one
    /// engine cannot serve two requests at once.
    /// </summary>
    public class MLImageClassifier : IImageClassifier
    {
        private readonly PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool;

        public MLImageClassifier(PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool)
        {
            this.predictionEnginePool = predictionEnginePool;
        }

        public Prediction Classify(byte[] image)
        {
            var output = predictionEnginePool.Predict(new ImageData { ImageSource = image });

            return new Prediction
            {
                Animal = output.PredictedLabel,
                Score = output.Score.Max()
            };
        }
    }
}
