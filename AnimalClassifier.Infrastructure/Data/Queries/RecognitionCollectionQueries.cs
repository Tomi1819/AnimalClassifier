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
            var animals = await Finds(Logs.Where(l => l.UserId == userId), minimumScore)
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
            var matching = Finds(Logs.InHistoryOf(userId, extensions), minimumScore)
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

        /// <summary>
        /// Each of the recognitions with the animal it finds, by the rule
        /// <see cref="IRecognitionCollectionQueries"/> sets out, which every
        /// query here reads through so that none can count by another. Among
        /// those that find an animal, one with feedback is one whose user said
        /// it shows that animal.
        /// </summary>
        private static IQueryable<Find> Finds(IQueryable<AnimalRecognitionLog> logs, float minimumScore) =>
            logs.Select(l => new Find
            {
                Id = l.Id,
                FileName = l.FileName,
                DateRecognized = l.DateRecognized,
                PredictionScore = l.PredictionScore,
                HasFeedback = l.Feedback != null,
                AnimalName = l.Feedback == null
                    ? (l.PredictionScore >= minimumScore ? l.AnimalName : null)
                    : l.Feedback.Verdict == FeedbackVerdict.Correct ? l.AnimalName
                    : l.Feedback.Verdict == FeedbackVerdict.WrongAnimal ? l.Feedback.ActualAnimal
                    : null
            });

        private sealed class Find
        {
            public int Id { get; init; }
            public string FileName { get; init; } = string.Empty;
            public DateTime DateRecognized { get; init; }
            public float PredictionScore { get; init; }
            public bool HasFeedback { get; init; }

            /// <summary>
            /// The animal it finds, as the recognition or its feedback names
            /// it; null when it finds none.
            /// </summary>
            public string? AnimalName { get; init; }
        }
    }
}
