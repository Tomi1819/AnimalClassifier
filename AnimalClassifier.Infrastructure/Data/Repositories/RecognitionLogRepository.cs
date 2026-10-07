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

        // What a search finds: every user's recognitions but the cleared ones,
        // of a file whose name ends with one of the extensions.
        private IQueryable<AnimalRecognitionLog> Searchable(IEnumerable<string> extensions) =>
            Logs.Where(l => !l.IsDeleted && extensions.Any(extension => l.FileName.EndsWith(extension)));

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

        public async Task<IReadOnlyList<(string AnimalName, int Count)>> CountByAnimalNameAsync(string term, IEnumerable<string> extensions, CancellationToken cancellationToken)
        {
            var animals = await Searchable(extensions)
                .Where(l => l.AnimalName.Contains(term))
                .GroupBy(l => l.AnimalName)
                .Select(g => new { AnimalName = g.Key, Count = g.Count() })
                .OrderByDescending(a => a.Count)
                .ThenBy(a => a.AnimalName)
                .ToListAsync(cancellationToken);

            return animals.Select(a => (a.AnimalName, a.Count)).ToList();
        }

        // EF Core cannot take a few from each group a GroupBy makes, but it
        // can from a query made for each distinct name, which it reads in one
        // go, numbering each animal's recognitions to keep the first few.
        public async Task<IReadOnlyList<AnimalRecognitionLog>> GetLatestByAnimalsAsync(IEnumerable<string> animalNames, IEnumerable<string> extensions, int countPerAnimal, CancellationToken cancellationToken)
        {
            var matching = Searchable(extensions).Where(l => animalNames.Contains(l.AnimalName));

            return await matching
                .Select(l => l.AnimalName)
                .Distinct()
                .SelectMany(animalName => matching
                    .Where(l => l.AnimalName == animalName)
                    .OrderByDescending(l => l.DateRecognized)
                    .ThenByDescending(l => l.Id)
                    .Take(countPerAnimal))
                .OrderBy(l => l.AnimalName)
                .ThenByDescending(l => l.DateRecognized)
                .ThenByDescending(l => l.Id)
                .ToListAsync(cancellationToken);
        }

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
