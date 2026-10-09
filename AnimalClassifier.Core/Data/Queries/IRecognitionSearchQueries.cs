namespace AnimalClassifier.Core.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// What the search finds among every user's recognitions.
    /// </summary>
    public interface IRecognitionSearchQueries
    {
        /// <summary>
        /// How often each animal whose name contains the term was recognised,
        /// by every user, in a file with one of the extensions, leaving out the
        /// cleared recognitions. Most recognised first, and ties alphabetically.
        /// Whether case matters is the database's collation's to say, and SQL
        /// Server's default ignores it.
        /// </summary>
        Task<IReadOnlyList<(string AnimalName, int Count)>> CountByAnimalNameAsync(string term, IEnumerable<string> extensions, CancellationToken cancellationToken);

        /// <summary>
        /// The most recent recognitions of each of the animals, by every user,
        /// in a file with one of the extensions, leaving out the cleared ones.
        /// Each animal's are together, most recent first.
        /// </summary>
        /// <param name="countPerAnimal">How many to read of each animal at most.</param>
        Task<IReadOnlyList<AnimalRecognitionLog>> GetLatestByAnimalsAsync(IEnumerable<string> animalNames, IEnumerable<string> extensions, int countPerAnimal, CancellationToken cancellationToken);
    }
}
