namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;

    public class AdminAuditLogRepository : IAdminAuditLogRepository
    {
        private readonly AnimalClassifierDbContext context;

        public AdminAuditLogRepository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public void Add(AdminAuditLog log) => context.AdminAuditLogs.Add(log);

        public async Task<(IReadOnlyList<AdminAuditLog> Logs, int TotalCount)> GetPageAsync(int page, int pageSize)
        {
            var totalCount = await context.AdminAuditLogs.CountAsync();
            var logs = await context.AdminAuditLogs
                .AsNoTracking()
                .Include(l => l.Admin)
                .Include(l => l.User)
                .OrderByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task DetachUserAsync(string userId)
        {
            await context.AdminAuditLogs
                .Where(l => l.AdminId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.AdminId, (string?)null));

            await context.AdminAuditLogs
                .Where(l => l.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.UserId, (string?)null));
        }
    }
}
