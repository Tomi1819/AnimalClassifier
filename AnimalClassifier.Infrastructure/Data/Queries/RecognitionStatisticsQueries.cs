namespace AnimalClassifier.Infrastructure.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Queries;
    using Microsoft.EntityFrameworkCore;

    // Reads only, untracked, since nothing changes what they return and the
    // context need not keep a copy of each row to compare against.
    public class RecognitionStatisticsQueries : IRecognitionStatisticsQueries
    {
        private readonly AnimalClassifierDbContext context;

        public RecognitionStatisticsQueries(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        private IQueryable<AnimalRecognitionLog> Logs => context.AnimalRecognitionLogs.AsNoTracking();

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
    }
}
