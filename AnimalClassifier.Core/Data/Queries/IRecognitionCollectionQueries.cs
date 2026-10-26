namespace AnimalClassifier.Core.Data.Queries
{
    /// <summary>
    /// What the collection is made of: the animals one user has found, and
    /// the image that shows each best.
    ///
    /// A recognition finds the animal its user says it shows: the one the
    /// model named when they said it was right, or the other they named in
    /// its place. One they said nothing of finds the animal the model named
    /// when the model was at least the minimum score sure of it, and none
    /// when it was less. One they said shows an animal the model does not
    /// know finds none. Whether case matters in a name is the database's
    /// collation's to say, and SQL Server's default ignores it.
    /// </summary>
    public interface IRecognitionCollectionQueries
    {
        /// <summary>
        /// Every animal the user found, with how many of their recognitions
        /// found it and when the first did, read across all of them, cleared
        /// ones and videos included.
        /// </summary>
        Task<IReadOnlyList<(string AnimalName, int Count, DateTime FirstFound)>> CountFindsByAnimalAsync(string userId, float minimumScore, CancellationToken cancellationToken);

        /// <summary>
        /// For each of the animals, the file of the user's recognition that
        /// shows it best, among those that found it in a file with one of the
        /// extensions, leaving out the cleared ones. One the user said shows
        /// the animal comes before one only the model was sure of; then the
        /// one the model was surest of, and of those equally sure, the most
        /// recent. An animal with no such recognition has none.
        /// </summary>
        Task<IReadOnlyList<(string AnimalName, string FileName)>> GetBestFindsAsync(string userId, IEnumerable<string> animalNames, IEnumerable<string> extensions, float minimumScore, CancellationToken cancellationToken);
    }
}
