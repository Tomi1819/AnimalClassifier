namespace AnimalClassifier.Infrastructure.Data.Queries
{
    using AnimalClassifier.Core.Data.Queries;
    using Microsoft.EntityFrameworkCore;

    // Reads only, untracked, since nothing changes what it returns and the
    // context need not keep a copy of each row to compare against. One row
    // a recognition, of a few short columns, as the rules need each in turn.
    public class RecognitionAchievementQueries : IRecognitionAchievementQueries
    {
        private readonly AnimalClassifierDbContext context;

        public RecognitionAchievementQueries(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public async Task<IReadOnlyList<RecognitionFacts>> GetFactsAsync(string userId, float minimumScore, CancellationToken cancellationToken) =>
            await context.AnimalRecognitionLogs.AsNoTracking()
                .Where(l => l.UserId == userId)
                .SelectFinds(minimumScore)
                .OrderBy(f => f.DateRecognized)
                .ThenBy(f => f.Id)
                .Select(f => new RecognitionFacts
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    DateRecognized = f.DateRecognized,
                    RecognizedAnimal = f.RecognizedAnimal,
                    PredictionScore = f.PredictionScore,
                    FoundAnimal = f.AnimalName,
                    Verdict = f.Verdict,
                    DateFeedbackSubmitted = f.DateFeedbackSubmitted,
                    DateAcceptedForTraining = f.DateAcceptedForTraining
                })
                .ToListAsync(cancellationToken);
    }
}
