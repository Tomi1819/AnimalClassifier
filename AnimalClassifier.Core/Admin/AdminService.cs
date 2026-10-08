namespace AnimalClassifier.Core.Admin
{
    using AnimalClassifier.Core.Admin.Models;
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using AnimalClassifier.Core.Identity;
    using Microsoft.AspNetCore.Identity;
    using static AnimalClassifier.Core.Admin.AdminMessages;
    using static AnimalClassifier.Core.Identity.RoleConstants;

    public class AdminService : IAdminService
    {
        private const int PageSize = 20;

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IUserRepository users;
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IAdminAuditLogRepository auditLogs;
        private readonly IUnitOfWork unitOfWork;

        public AdminService(UserManager<ApplicationUser> userManager,
                            IUserRepository users,
                            IRecognitionLogRepository recognitionLogs,
                            IAdminAuditLogRepository auditLogs,
                            IUnitOfWork unitOfWork)
        {
            this.userManager = userManager;
            this.users = users;
            this.recognitionLogs = recognitionLogs;
            this.auditLogs = auditLogs;
            this.unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<AdminUserItem>> GetUsersAsync(string? search, int page, CancellationToken cancellationToken)
        {
            var (pageOfUsers, totalCount) = await users.GetPageAsync(search, page, PageSize, cancellationToken);
            var recognitionCounts = await recognitionLogs.CountHistoryByUserAsync(pageOfUsers.Select(u => u.Id), cancellationToken);
            var adminIds = (await userManager.GetUsersInRoleAsync(Admin)).Select(u => u.Id).ToHashSet();

            return new PagedResult<AdminUserItem>
            {
                Items = pageOfUsers.Select(user => new AdminUserItem
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    DateRegistered = user.DateRegistered,
                    IsAdmin = adminIds.Contains(user.Id),
                    IsLocked = user.LockoutEnd > DateTimeOffset.UtcNow,
                    RecognitionCount = recognitionCounts.GetValueOrDefault(user.Id)
                }).ToList(),
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount
            };
        }

        public Task LockUserAsync(string adminId, string userId) =>
            ChangeUserAsync(adminId, userId, AdminAction.Lock,
                user => user.LockoutEnd == DateTimeOffset.MaxValue
                    ? throw new RequestRefusedException(UserAlreadyLocked)
                    : userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue));

        public Task UnlockUserAsync(string adminId, string userId) =>
            ChangeUserAsync(adminId, userId, AdminAction.Unlock,
                user => user.LockoutEnd > DateTimeOffset.UtcNow
                    ? userManager.SetLockoutEndDateAsync(user, null)
                    : throw new RequestRefusedException(UserNotLocked));

        public Task GrantAdminAsync(string adminId, string userId) =>
            ChangeUserAsync(adminId, userId, AdminAction.GrantAdmin,
                user => userManager.AddToRoleAsync(user, Admin));

        public Task RevokeAdminAsync(string adminId, string userId) =>
            ChangeUserAsync(adminId, userId, AdminAction.RevokeAdmin,
                user => userManager.RemoveFromRoleAsync(user, Admin));

        public async Task<PagedResult<AdminAuditLogItem>> GetAuditLogAsync(int page, CancellationToken cancellationToken)
        {
            var (logs, totalCount) = await auditLogs.GetPageAsync(page, PageSize, cancellationToken);

            return new PagedResult<AdminAuditLogItem>
            {
                Items = logs.Select(log => new AdminAuditLogItem
                {
                    Action = log.Action.ToString(),
                    DatePerformed = log.DatePerformed,
                    AdminEmail = DescribeAccount(log.Admin),
                    UserEmail = DescribeAccount(log.User)
                }).ToList(),
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount
            };
        }

        private async Task ChangeUserAsync(string adminId, string userId, AdminAction action, Func<ApplicationUser, Task<IdentityResult>> change)
        {
            if (adminId == userId)
            {
                throw new RequestRefusedException(OwnAccountChange);
            }

            var user = await userManager.GetByIdAsync(userId);

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                (await change(user)).ThrowIfFailed();

                // Identity leaves the stamp alone on role and lockout changes, and
                // the stamp is what ends the user's current sessions.
                (await userManager.UpdateSecurityStampAsync(user)).ThrowIfFailed();

                auditLogs.Add(new AdminAuditLog
                {
                    Action = action,
                    DatePerformed = DateTime.UtcNow,
                    AdminId = adminId,
                    UserId = userId
                });
                await unitOfWork.SaveChangesAsync();
            });
        }

        // An entry outlives the accounts it names, which are gone once deleted.
        private static string DescribeAccount(ApplicationUser? account) =>
            account is null ? DeletedUser : account.Email ?? string.Empty;
    }
}
