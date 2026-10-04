namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;
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

        public Task<int> CountAsync(CancellationToken cancellationToken) =>
            Logs.CountAsync(cancellationToken);

        public Task<int> CountUsersAsync(CancellationToken cancellationToken) =>
            Logs.Select(l => l.UserId).Distinct().CountAsync(cancellationToken);

        public async Task<IReadOnlyList<(string AnimalName, int Count)>> GetMostRecognisedAnimalsAsync(int count, CancellationToken cancellationToken)
        {
            // Ties are put in alphabetical order, so that the same counts
            // always list the same animals.
            var animals = await Logs
                .GroupBy(l => l.AnimalName)
                .Select(g => new { AnimalName = g.Key, Count = g.Count() })
                .OrderByDescending(a => a.Count)
                .ThenBy(a => a.AnimalName)
                .Take(count)
                .ToListAsync(cancellationToken);

            return animals.Select(a => (a.AnimalName, a.Count)).ToList();
        }

        public async Task<IReadOnlyList<DateTime>> GetDatesSinceAsync(DateTime since, CancellationToken cancellationToken) =>
            await Logs.Where(l => l.DateRecognized >= since)
                      .Select(l => l.DateRecognized)
                      .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<AnimalRecognitionLog>> FindByAnimalNameAsync(string term, CancellationToken cancellationToken) =>
            await Logs.Where(l => l.AnimalName.Contains(term) && !l.IsDeleted)
                      .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<AnimalRecognitionLog>> GetHistoryAsync(string userId, CancellationToken cancellationToken) =>
            await Logs.Where(l => l.UserId == userId && !l.IsDeleted)
                      .Include(l => l.Feedback)
                      .OrderByDescending(l => l.DateRecognized)
                      .ToListAsync(cancellationToken);

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
