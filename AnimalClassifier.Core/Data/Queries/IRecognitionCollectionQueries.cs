namespace AnimalClassifier.Core.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// What the collection is made of: the animals one user has recognised,
    /// and the image that shows each best.
    /// </summary>
    public interface IRecognitionCollectionQueries
    {
        /// <summary>
        /// Every animal the user recognised, with how often and when first,
        /// read across all of their recognitions, cleared ones and videos
        /// included. Whether case matters is the database's collation's to
        /// say, and SQL Server's default ignores it.
        /// </summary>
        Task<IReadOnlyList<(string AnimalName, int Count, DateTime FirstRecognized)>> CountByAnimalAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// For each of the animals, the user's recognition the model was
        /// surest of, in a file with one of the extensions, leaving out the
        /// cleared ones. Equal scores go to the most recent. An animal with no
        /// such recognition has none.
        /// </summary>
        Task<IReadOnlyList<AnimalRecognitionLog>> GetBestByAnimalsAsync(string userId, IEnumerable<string> animalNames, IEnumerable<string> extensions, CancellationToken cancellationToken);
    }
}
