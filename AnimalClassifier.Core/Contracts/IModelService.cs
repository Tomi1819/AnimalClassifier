namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    public interface IModelService
    {
        string PredictAnimal(ModelInput input);
    }
}
