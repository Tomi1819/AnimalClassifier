namespace AnimalClassifier.Core.Recognitions.Collection
{
    using AnimalClassifier.Core.Recognitions.Collection.Models;

    /// <summary>
    /// The animals the model knows, as a collection one user fills by
    /// recognising them. An animal counts as found once it is recognised,
    /// in an image or a video, and stays found when the history is cleared,
    /// as the statistics do. No other user's recognitions change it.
    /// </summary>
    public interface IAnimalCollectionService
    {
        Task<CollectionProgress> GetCollectionAsync(string userId, CancellationToken cancellationToken);
    }
}
