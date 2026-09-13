namespace AnimalClassifier.Infrastructure.Data.Common
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore.Storage;
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

        /// <summary>
        /// One page of the users whose email or name contains the search term,
        /// newest first, with the number of users that match.
        /// </summary>
        Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> GetUsersAsync(string? search, int page, int pageSize);

        /// <summary>
        /// The number of recognitions in each of the given users' history, so
        /// cleared ones are left out, as are users without any.
        /// </summary>
        Task<Dictionary<string, int>> CountRecognitionLogsByUserAsync(IEnumerable<string> userIds);
        Task AddAdminAuditLogAsync(AdminAuditLog adminAuditLog);

        /// <summary>
        /// One page of the audit log, most recent first, with the total number of entries.
        /// </summary>
        Task<(IEnumerable<AdminAuditLog> Logs, int TotalCount)> GetAdminAuditLogsAsync(int page, int pageSize);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task SaveChangesAsync();
    }
}
