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

        public async Task<IReadOnlyList<(string AnimalName, int Count, DateTime FirstFound)>> CountFindsByAnimalAsync(string userId, float minimumScore, CancellationToken cancellationToken)
        {
            var animals = await Logs.Where(l => l.UserId == userId).SelectFinds(minimumScore)
                .Where(f => f.AnimalName != null)
                .GroupBy(f => f.AnimalName!)
                .Select(g => new { AnimalName = g.Key, Count = g.Count(), FirstFound = g.Min(f => f.DateRecognized) })
                .ToListAsync(cancellationToken);

            return animals.Select(a => (a.AnimalName, a.Count, a.FirstFound)).ToList();
        }

        // EF Core cannot take the first of each group a GroupBy makes, but it
        // can from a query made for each distinct name, which it reads in one
        // go, as the search does.
        public async Task<IReadOnlyList<(string AnimalName, string FileName)>> GetBestFindsAsync(string userId, IEnumerable<string> animalNames, IEnumerable<string> extensions, float minimumScore, CancellationToken cancellationToken)
        {
            var matching = Logs.InHistoryOf(userId, extensions).SelectFinds(minimumScore)
                .Where(f => f.AnimalName != null && animalNames.Contains(f.AnimalName));

            var best = await matching
                .Select(f => f.AnimalName)
                .Distinct()
                .SelectMany(animalName => matching
                    .Where(f => f.AnimalName == animalName)
                    .OrderByDescending(f => f.HasFeedback)
                    .ThenByDescending(f => f.PredictionScore)
                    .ThenByDescending(f => f.DateRecognized)
                    .ThenByDescending(f => f.Id)
                    .Take(1))
                .Select(f => new { AnimalName = f.AnimalName!, f.FileName })
                .ToListAsync(cancellationToken);

            return best.Select(b => (b.AnimalName, b.FileName)).ToList();
        }

        public async Task<(string RecognizedAnimal, string? FoundAnimal, bool HasFeedback, bool IsFirstFind)?> GetFindAsync(string userId, int recognitionId, float minimumScore, CancellationToken cancellationToken)
        {
            var finds = Logs.Where(l => l.UserId == userId).SelectFinds(minimumScore);

            var find = await finds
                .Where(f => f.Id == recognitionId)
                .Select(f => new
                {
                    f.RecognizedAnimal,
                    f.AnimalName,
                    f.HasFeedback,
                    IsFirstFind = f.AnimalName != null && !finds.Any(earlier =>
                        earlier.AnimalName == f.AnimalName
                        && (earlier.DateRecognized < f.DateRecognized
                            || (earlier.DateRecognized == f.DateRecognized && earlier.Id < f.Id)))
                })
                .SingleOrDefaultAsync(cancellationToken);

            return find is null ? null : (find.RecognizedAnimal, find.AnimalName, find.HasFeedback, find.IsFirstFind);
        }
    }
}
