namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;

    /// <summary>
    /// What administrators have done to users. An entry outlives the accounts
    /// it names, so the log still covers everything that was done.
    /// </summary>
    public interface IAdminAuditLogRepository
    {
        /// <summary>
        /// Adds an entry, which is stored on the next save.
        /// </summary>
        void Add(AdminAuditLog log);

        /// <summary>
        /// One page of the log, most recent first, with the accounts each
        /// entry names and the total number of entries.
        /// </summary>
        Task<(IReadOnlyList<AdminAuditLog> Logs, int TotalCount)> GetPageAsync(int page, int pageSize);

        /// <summary>
        /// Takes one user out of the log, whether they made a change or had
        /// one made to them, so that their account can be deleted. The entries
        /// stay, naming nobody in that place. Takes effect at once, without a
        /// save.
        /// </summary>
        Task DetachUserAsync(string userId);
    }
}
