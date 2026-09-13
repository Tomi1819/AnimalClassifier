namespace AnimalClassifier.Infrastructure.Data.Common
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    public class Repository : IRepository
    {
        private readonly AnimalClassifierDbContext context;

        public Repository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public async Task AddRecognitionLogAsync(AnimalRecognitionLog animalRecognitionLog)
        {
            await context.AnimalRecognitionLogs.AddAsync(animalRecognitionLog);
        }

        public async Task<IEnumerable<AnimalRecognitionLog>> GetAllRecognitionLogsAsync()
        {
            return await context.AnimalRecognitionLogs.ToListAsync();
        }

        public async Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id)
        {
            return await context.AnimalRecognitionLogs.FindAsync(id);
        }

        public async Task<IEnumerable<AnimalRecognitionLog>> GetRecognitionLogsForUserAsync(string userId)
        {
            return await context.AnimalRecognitionLogs
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .OrderByDescending(l => l.DateRecognized)
                .ToListAsync();
        }

        public async Task<int> ClearRecognitionLogsForUserAsync(string userId)
        {
            var logs = await context.AnimalRecognitionLogs
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .ToListAsync();

            foreach (var log in logs)
            {
                log.IsDeleted = true;
            }

            await context.SaveChangesAsync();

            return logs.Count;
        }

        public async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> GetUsersAsync(string? search, int page, int pageSize)
        {
            var users = context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u => u.Email!.Contains(search) || u.FullName.Contains(search));
            }

            var totalCount = await users.CountAsync();
            var pageOfUsers = await users
                .OrderByDescending(u => u.DateRegistered)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (pageOfUsers, totalCount);
        }

        public async Task<Dictionary<string, int>> CountRecognitionLogsByUserAsync(IEnumerable<string> userIds)
        {
            return await context.AnimalRecognitionLogs
                .Where(l => userIds.Contains(l.UserId) && !l.IsDeleted)
                .GroupBy(l => l.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count);
        }

        public async Task AddAdminAuditLogAsync(AdminAuditLog adminAuditLog)
        {
            await context.AdminAuditLogs.AddAsync(adminAuditLog);
        }

        public async Task<(IEnumerable<AdminAuditLog> Logs, int TotalCount)> GetAdminAuditLogsAsync(int page, int pageSize)
        {
            var totalCount = await context.AdminAuditLogs.CountAsync();
            var logs = await context.AdminAuditLogs
                .Include(l => l.Admin)
                .Include(l => l.User)
                .OrderByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await context.Database.BeginTransactionAsync();
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
