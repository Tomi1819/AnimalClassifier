namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Recognitions.Classification.Models;
    using Microsoft.Extensions.ML;
    using Microsoft.ML.Data;

    /// <summary>
    /// The trained model, run through a pool of prediction engines, since one
    /// engine cannot serve two requests at once.
    /// </summary>
    public class MLImageClassifier : IImageClassifier
    {
        private const string ScoreColumn = "Score";

        private readonly PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool;

        // Read once, on first use, as the model is loaded once and never
        // changes while the app runs.
        private readonly Lazy<IReadOnlyList<string>> knownAnimals;

        public MLImageClassifier(PredictionEnginePool<ImageData, ImagePrediction> predictionEnginePool)
        {
            this.predictionEnginePool = predictionEnginePool;
            knownAnimals = new Lazy<IReadOnlyList<string>>(ReadKnownAnimals);
        }

        public IReadOnlyList<string> KnownAnimals => knownAnimals.Value;

        public Prediction Classify(byte[] image)
        {
            var output = predictionEnginePool.Predict(new ImageData { ImageSource = image });

            return new Prediction
            {
                Animal = output.PredictedLabel,
                Score = output.Score.Max()
            };
        }

        // The model names each of its scores after the animal it is for, so
        // the names are the animals it was trained on.
        private IReadOnlyList<string> ReadKnownAnimals()
        {
            var engine = predictionEnginePool.GetPredictionEngine();

            try
            {
                var names = default(VBuffer<ReadOnlyMemory<char>>);
                engine.OutputSchema[ScoreColumn].GetSlotNames(ref names);

                return names.DenseValues()
                            .Select(name => name.ToString())
                            .Order(StringComparer.OrdinalIgnoreCase)
                            .ToList();
            }
            finally
            {
                predictionEnginePool.ReturnPredictionEngine(engine);
            }
        }
    }
}
