namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Extensions;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using static Constants.MessageConstants;
    using static Constants.RoleConstants;

    public class AdminService : IAdminService
    {
        private const int PageSize = 20;

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IRepository repository;

        public AdminService(UserManager<ApplicationUser> userManager, IRepository repository)
        {
            this.userManager = userManager;
            this.repository = repository;
        }

        public async Task<PagedResult<AdminUserItem>> GetUsersAsync(string? search, int page)
        {
            var (users, totalCount) = await repository.GetUsersAsync(search, page, PageSize);
            var recognitionCounts = await repository.CountRecognitionLogsByUserAsync(users.Select(u => u.Id));
            var adminIds = (await userManager.GetUsersInRoleAsync(Admin)).Select(u => u.Id).ToHashSet();

            return new PagedResult<AdminUserItem>
            {
                Items = users.Select(user => new AdminUserItem
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
                    ? throw new InvalidOperationException(UserAlreadyLocked)
                    : userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue));

        public Task UnlockUserAsync(string adminId, string userId) =>
            ChangeUserAsync(adminId, userId, AdminAction.Unlock,
                user => user.LockoutEnd > DateTimeOffset.UtcNow
                    ? userManager.SetLockoutEndDateAsync(user, null)
                    : throw new InvalidOperationException(UserNotLocked));

        public Task GrantAdminAsync(string adminId, string userId) =>
            ChangeUserAsync(adminId, userId, AdminAction.GrantAdmin,
                user => userManager.AddToRoleAsync(user, Admin));

        public Task RevokeAdminAsync(string adminId, string userId) =>
            ChangeUserAsync(adminId, userId, AdminAction.RevokeAdmin,
                user => userManager.RemoveFromRoleAsync(user, Admin));

        public async Task<PagedResult<AdminAuditLogItem>> GetAuditLogAsync(int page)
        {
            var (logs, totalCount) = await repository.GetAdminAuditLogsAsync(page, PageSize);

            return new PagedResult<AdminAuditLogItem>
            {
                Items = logs.Select(log => new AdminAuditLogItem
                {
                    Action = log.Action.ToString(),
                    DatePerformed = log.DatePerformed,
                    AdminEmail = log.Admin.Email ?? string.Empty,
                    UserEmail = log.User.Email ?? string.Empty
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
                throw new InvalidOperationException(OwnAccountChange);
            }

            var user = await userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException(UserNotFound);

            await using var transaction = await repository.BeginTransactionAsync();

            (await change(user)).ThrowIfFailed();

            // Identity leaves the stamp alone on role and lockout changes, and the
            // stamp is what ends the user's current sessions.
            (await userManager.UpdateSecurityStampAsync(user)).ThrowIfFailed();

            await repository.AddAdminAuditLogAsync(new AdminAuditLog
            {
                Action = action,
                DatePerformed = DateTime.UtcNow,
                AdminId = adminId,
                UserId = userId
            });
            await repository.SaveChangesAsync();

            await transaction.CommitAsync();
        }
    }
}
