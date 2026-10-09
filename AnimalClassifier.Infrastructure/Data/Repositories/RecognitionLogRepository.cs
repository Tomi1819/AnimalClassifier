namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using Microsoft.EntityFrameworkCore;

    // Reads are untracked, since nothing changes what they return and the
    // context need not keep a copy of each row to compare against.
    public class RecognitionLogRepository : IRecognitionLogRepository
    {
        private readonly AnimalClassifierDbContext context;

        public RecognitionLogRepository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        private IQueryable<AnimalRecognitionLog> Logs => context.AnimalRecognitionLogs.AsNoTracking();

        public void Add(AnimalRecognitionLog log) => context.AnimalRecognitionLogs.Add(log);

        public Task<AnimalRecognitionLog?> GetForUserAsync(string userId, int id, CancellationToken cancellationToken) =>
            Logs.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, cancellationToken);

        public async Task<(IReadOnlyList<AnimalRecognitionLog> Logs, int TotalCount)> GetHistoryPageAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var history = Logs.Where(l => l.UserId == userId && !l.IsDeleted);

            var totalCount = await history.CountAsync(cancellationToken);
            var logs = await history
                .Include(l => l.Feedback)
                .OrderByDescending(l => l.DateRecognized)
                .ThenByDescending(l => l.Id)
                .TakePage(page, pageSize)
                .ToListAsync(cancellationToken);

            return (logs, totalCount);
        }

        public async Task<IReadOnlyList<AnimalRecognitionLog>> GetAllForUserAsync(string userId) =>
            await Logs.Where(l => l.UserId == userId)
                      .Include(l => l.Feedback)
                      .OrderByDescending(l => l.DateRecognized)
                      .ToListAsync();

        public Task<Dictionary<string, int>> CountHistoryByUserAsync(IEnumerable<string> userIds, CancellationToken cancellationToken) =>
            Logs.Where(l => userIds.Contains(l.UserId) && !l.IsDeleted)
                .GroupBy(l => l.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

        public Task<int> ClearHistoryAsync(string userId) =>
            context.AnimalRecognitionLogs
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.IsDeleted, true));

        public Task DeleteAllForUserAsync(string userId) =>
            context.AnimalRecognitionLogs
                .Where(l => l.UserId == userId)
                .ExecuteDeleteAsync();
    }
}
