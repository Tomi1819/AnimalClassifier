namespace AnimalClassifier.Core.Admin
{
    using AnimalClassifier.Core.Admin.Models;
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;

    /// <summary>
    /// Changes to a user end their sessions, so they apply from the user's next
    /// request, and are recorded in the audit log. An administrator cannot
    /// change their own account, and a change that would have no effect is
    /// refused.
    /// </summary>
    public interface IAdminService
    {
        /// <summary>
        /// One page of the users whose email or name contains the search term,
        /// newest first.
        /// </summary>
        Task<PagedResult<AdminUserItem>> GetUsersAsync(string? search, int page, CancellationToken cancellationToken);

        /// <exception cref="RequestRefusedException">
        /// When it is the administrator's own account, or it is locked already.
        /// </exception>
        /// <exception cref="NotFoundException">When the user does not exist.</exception>
        Task LockUserAsync(string adminId, string userId);

        /// <exception cref="RequestRefusedException">
        /// When it is the administrator's own account, or it is not locked.
        /// </exception>
        /// <exception cref="NotFoundException">When the user does not exist.</exception>
        Task UnlockUserAsync(string adminId, string userId);

        /// <exception cref="RequestRefusedException">
        /// When it is the administrator's own account, or it is one already.
        /// </exception>
        /// <exception cref="NotFoundException">When the user does not exist.</exception>
        Task GrantAdminAsync(string adminId, string userId);

        /// <exception cref="RequestRefusedException">
        /// When it is the administrator's own account, or it is not one.
        /// </exception>
        /// <exception cref="NotFoundException">When the user does not exist.</exception>
        Task RevokeAdminAsync(string adminId, string userId);

        /// <summary>
        /// One page of the audit log, most recent first.
        /// </summary>
        Task<PagedResult<AdminAuditLogItem>> GetAuditLogAsync(int page, CancellationToken cancellationToken);
    }
}
