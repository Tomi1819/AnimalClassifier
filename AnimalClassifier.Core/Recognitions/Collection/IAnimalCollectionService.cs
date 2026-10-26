namespace AnimalClassifier.Core.Recognitions.Collection
{
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Collection.Models;

    /// <summary>
    /// The animals the model knows, as a collection one user fills by finding
    /// them. A recognition finds an animal when the model was sure enough of
    /// it, as <see cref="CollectionSettings.MinimumScore"/> sets, or when the
    /// user says it shows that animal, whatever the model named; see
    /// <see cref="IRecognitionCollectionQueries"/> for the whole rule. Images
    /// and videos both find animals, and one found stays found when the
    /// history is cleared, as the statistics do. No other user's recognitions
    /// change it.
    /// </summary>
    public interface IAnimalCollectionService
    {
        Task<CollectionProgress> GetCollectionAsync(string userId, CancellationToken cancellationToken);
    }
}
