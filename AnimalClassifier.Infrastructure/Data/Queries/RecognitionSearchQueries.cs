namespace AnimalClassifier.Infrastructure.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Queries;
    using Microsoft.EntityFrameworkCore;

    // Reads only, untracked, since nothing changes what they return and the
    // context need not keep a copy of each row to compare against.
    public class RecognitionSearchQueries : IRecognitionSearchQueries
    {
        private readonly AnimalClassifierDbContext context;

        public RecognitionSearchQueries(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        private IQueryable<AnimalRecognitionLog> Logs => context.AnimalRecognitionLogs.AsNoTracking();

        // What a search finds: every user's recognitions but the cleared ones,
        // of a file whose name ends with one of the extensions. The match is
        // SQL's LIKE, so the column's collation compares; the extensions are
        // MediaFile's, none of which holds a character LIKE treats as a wildcard.
        private IQueryable<AnimalRecognitionLog> Searchable(IEnumerable<string> extensions) =>
            Logs.Where(l => !l.IsDeleted && extensions.Any(extension => EF.Functions.Like(l.FileName, "%" + extension)));

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
    }
}
