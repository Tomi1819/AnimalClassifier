namespace AnimalClassifier.Core.Data.Repositories
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// Every recognition made. One a user clears from their history is kept,
    /// marked as cleared, so the statistics, which read every recognition,
    /// still count it.
    ///
    /// Counting them across every user is IRecognitionStatisticsQueries' to
    /// do, and searching them IRecognitionSearchQueries'.
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
