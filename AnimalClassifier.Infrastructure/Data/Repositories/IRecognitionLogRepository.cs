namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;

    /// <summary>
    /// Every recognition made. One a user clears from their history is kept,
    /// marked as cleared, so the statistics and search pages, which read every
    /// recognition, still count it.
    /// </summary>
    public interface IRecognitionLogRepository
    {
        /// <summary>
        /// Adds a recognition, which is stored on the next save.
        /// </summary>
        void Add(AnimalRecognitionLog log);

        Task<AnimalRecognitionLog?> GetByIdAsync(int id);

        Task<IReadOnlyList<AnimalRecognitionLog>> GetAllAsync();

        Task<IReadOnlyList<DateTime>> GetDatesSinceAsync(DateTime since);

        /// <summary>
        /// The recognitions belonging to one user that they have not cleared,
        /// most recent first.
        /// </summary>
        Task<IReadOnlyList<AnimalRecognitionLog>> GetHistoryAsync(string userId);

        /// <summary>
        /// Every recognition one user made, cleared ones included, most recent
        /// first.
        /// </summary>
        Task<IReadOnlyList<AnimalRecognitionLog>> GetAllForUserAsync(string userId);

        /// <summary>
        /// The number of recognitions in each of the given users' history, so
        /// cleared ones are left out, as are users without any.
        /// </summary>
        Task<Dictionary<string, int>> CountHistoryByUserAsync(IEnumerable<string> userIds);

        /// <summary>
        /// Marks one user's recognitions as cleared, and returns how many were
        /// affected. Nothing is removed. Takes effect at once, without a save.
        /// </summary>
        Task<int> ClearHistoryAsync(string userId);

        /// <summary>
        /// Removes every recognition one user made, cleared ones included, so
        /// that they leave the statistics and search pages as well. Unlike
        /// clearing, this cannot be undone, and is meant for an account that is
        /// itself going. Takes effect at once, without a save.
        /// </summary>
        Task DeleteAllForUserAsync(string userId);
    }
}
