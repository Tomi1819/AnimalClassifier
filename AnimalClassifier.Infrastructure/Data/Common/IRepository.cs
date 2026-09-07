namespace AnimalClassifier.Infrastructure.Data.Common
{
    using AnimalClassifier.Infrastructure.Data.Models;
    public interface IRepository
    {
        Task<IEnumerable<AnimalRecognitionLog>> GetAllRecognitionLogsAsync();
        Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id);

        /// <summary>
        /// The recognitions belonging to one user that they have not cleared,
        /// most recent first.
        /// </summary>
        Task<IEnumerable<AnimalRecognitionLog>> GetRecognitionLogsForUserAsync(string userId);

        /// <summary>
        /// Marks one user's recognitions as cleared, and returns how many were
        /// affected. Nothing is removed, so the statistics and search pages,
        /// which read every log, are unchanged.
        /// </summary>
        Task<int> ClearRecognitionLogsForUserAsync(string userId);
        Task AddRecognitionLogAsync(AnimalRecognitionLog animalRecognitionLog);
        Task SaveChangesAsync();
    }
}
