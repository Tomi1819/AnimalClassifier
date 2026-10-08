namespace AnimalClassifier.Core.Data.Repositories
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// Every recognition made. One a user clears from their history is kept,
    /// marked as cleared, so the statistics, which read every recognition,
    /// still count it.
    /// </summary>
    public interface IRecognitionLogRepository
    {
        /// <summary>
        /// Adds a recognition, which is stored on the next save.
        /// </summary>
        void Add(AnimalRecognitionLog log);

        /// <summary>
        /// One recognition, if it exists and belongs to the user.
        /// </summary>
        Task<AnimalRecognitionLog?> GetForUserAsync(string userId, int id, CancellationToken cancellationToken);

        Task<int> CountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// How many users have made a recognition, cleared ones included.
        /// </summary>
        Task<int> CountUsersAsync(CancellationToken cancellationToken);

        /// <summary>
        /// The animals recognised most, with how often each was, most first.
        /// </summary>
        /// <param name="count">How many animals to read.</param>
        Task<IReadOnlyList<(string AnimalName, int Count)>> GetMostRecognisedAnimalsAsync(int count, CancellationToken cancellationToken);

        /// <summary>
        /// When each recognition since the date was made, and nothing else
        /// about them.
        /// </summary>
        Task<IReadOnlyList<DateTime>> GetDatesSinceAsync(DateTime since, CancellationToken cancellationToken);

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

        /// <summary>
        /// One page of the recognitions belonging to one user that they have
        /// not cleared, most recent first, each with any feedback they gave on
        /// it, and how many there are on every page together.
        /// </summary>
        /// <param name="page">Which page, counted from 1.</param>
        Task<(IReadOnlyList<AnimalRecognitionLog> Logs, int TotalCount)> GetHistoryPageAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>
        /// Every recognition one user made, cleared ones included, most recent
        /// first, each with any feedback they gave on it.
        /// </summary>
        Task<IReadOnlyList<AnimalRecognitionLog>> GetAllForUserAsync(string userId);

        /// <summary>
        /// The number of recognitions in each of the given users' history, so
        /// cleared ones are left out, as are users without any.
        /// </summary>
        Task<Dictionary<string, int>> CountHistoryByUserAsync(IEnumerable<string> userIds, CancellationToken cancellationToken);

        /// <summary>
        /// Marks one user's recognitions as cleared, and returns how many were
        /// affected. Nothing is removed. Takes effect at once, without a save.
        /// </summary>
        Task<int> ClearHistoryAsync(string userId);

        /// <summary>
        /// Removes every recognition one user made, cleared ones included, so
        /// that they leave the statistics and search pages as well, and the
        /// feedback given on them with them. Unlike
        /// clearing, this cannot be undone, and is meant for an account that is
        /// itself going. Takes effect at once, without a save.
        /// </summary>
        Task DeleteAllForUserAsync(string userId);
    }
}
