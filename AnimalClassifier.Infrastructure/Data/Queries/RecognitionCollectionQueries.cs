namespace AnimalClassifier.Infrastructure.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Queries;
    using Microsoft.EntityFrameworkCore;

    // Reads only, untracked, since nothing changes what they return and the
    // context need not keep a copy of each row to compare against.
    public class RecognitionCollectionQueries : IRecognitionCollectionQueries
    {
        private readonly AnimalClassifierDbContext context;

        public RecognitionCollectionQueries(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        private IQueryable<AnimalRecognitionLog> Logs => context.AnimalRecognitionLogs.AsNoTracking();

        public async Task<IReadOnlyList<(string AnimalName, int Count, DateTime FirstRecognized)>> CountByAnimalNameAsync(string userId, CancellationToken cancellationToken)
        {
            var animals = await Logs
                .Where(l => l.UserId == userId)
                .GroupBy(l => l.AnimalName)
                .Select(g => new { AnimalName = g.Key, Count = g.Count(), FirstRecognized = g.Min(l => l.DateRecognized) })
                .ToListAsync(cancellationToken);

            return animals.Select(a => (a.AnimalName, a.Count, a.FirstRecognized)).ToList();
        }

        // EF Core cannot take the first of each group a GroupBy makes, but it
        // can from a query made for each distinct name, which it reads in one
        // go, as the search does.
        public async Task<IReadOnlyList<AnimalRecognitionLog>> GetBestByAnimalsAsync(string userId, IEnumerable<string> animalNames, IEnumerable<string> extensions, CancellationToken cancellationToken)
        {
            var matching = Logs.InHistoryOf(userId, extensions).Where(l => animalNames.Contains(l.AnimalName));

            return await matching
                .Select(l => l.AnimalName)
                .Distinct()
                .SelectMany(animalName => matching
                    .Where(l => l.AnimalName == animalName)
                    .OrderByDescending(l => l.PredictionScore)
                    .ThenByDescending(l => l.DateRecognized)
                    .ThenByDescending(l => l.Id)
                    .Take(1))
                .ToListAsync(cancellationToken);
        }
    }
}
