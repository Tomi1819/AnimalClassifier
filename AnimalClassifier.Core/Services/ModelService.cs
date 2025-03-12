namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.ML;

    public class ModelService : IModelService
    {
        private readonly MLContext mLContext;
        private ITransformer model;
        private readonly PredictionEngine<ModelInput, ModelOutput> predictionEngine;
        private readonly string modelPath;

        public ModelService(IConfiguration configuration)
        {
            this.mLContext = new MLContext();
            this.modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModels", "MLModel.mlnet");
            this.model = mLContext.Model.Load(modelPath, out var modelInputSchema);
            predictionEngine = mLContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(this.model);
        }
        public string PredictAnimal(ModelInput input)
        {
            var prediction = predictionEngine.Predict(input);
            return prediction.PredictedLabel;
        }
    }
}
