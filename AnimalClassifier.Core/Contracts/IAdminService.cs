namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;

    /// <summary>
    /// Changes to a user end their sessions, so they apply from the user's next
    /// request, and are recorded in the audit log. An administrator cannot
    /// change their own account, and a change that would have no effect is
    /// refused.
    /// </summary>
    public interface IAdminService
    {
        Task<PagedResult<AdminUserItem>> GetUsersAsync(string? search, int page);
        Task LockUserAsync(string adminId, string userId);
        Task UnlockUserAsync(string adminId, string userId);
        Task GrantAdminAsync(string adminId, string userId);
        Task RevokeAdminAsync(string adminId, string userId);
        Task<PagedResult<AdminAuditLogItem>> GetAuditLogAsync(int page);
    }
}
