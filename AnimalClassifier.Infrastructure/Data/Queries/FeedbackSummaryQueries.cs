namespace AnimalClassifier.Infrastructure.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Queries;
    using Microsoft.EntityFrameworkCore;

    public class FeedbackSummaryQueries : IFeedbackSummaryQueries
    {
        private readonly AnimalClassifierDbContext context;

        public FeedbackSummaryQueries(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public Task<Dictionary<FeedbackVerdict, int>> CountByVerdictAsync(CancellationToken cancellationToken) =>
            context.RecognitionFeedback
                .GroupBy(f => f.Verdict)
                .Select(g => new { Verdict = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Verdict, g => g.Count, cancellationToken);

        public Task<Dictionary<FeedbackReviewStatus, int>> CountForReviewByStatusAsync(CancellationToken cancellationToken) =>
            context.RecognitionFeedback.ForReview()
                .GroupBy(f => f.ReviewStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        public async Task<IReadOnlyList<(string RecognizedAnimal, string ActualAnimal, int Count)>> GetMostCommonMistakesAsync(int count, CancellationToken cancellationToken)
        {
            // Ties are put in alphabetical order, so that the same counts
            // always list the same mistakes.
            var mistakes = await context.RecognitionFeedback
                .Where(f => f.Verdict == FeedbackVerdict.WrongAnimal)
                .GroupBy(f => new { RecognizedAnimal = f.Recognition.AnimalName, ActualAnimal = f.ActualAnimal! })
                .Select(g => new { g.Key.RecognizedAnimal, g.Key.ActualAnimal, Count = g.Count() })
                .OrderByDescending(m => m.Count)
                .ThenBy(m => m.RecognizedAnimal)
                .ThenBy(m => m.ActualAnimal)
                .Take(count)
                .ToListAsync(cancellationToken);

            return mistakes.Select(m => (m.RecognizedAnimal, m.ActualAnimal, m.Count)).ToList();
        }

        public async Task<IReadOnlyList<(string Animal, int Count)>> GetMostNamedUnlistedAnimalsAsync(int count, CancellationToken cancellationToken)
        {
            var animals = await context.RecognitionFeedback
                .Where(f => f.Verdict == FeedbackVerdict.UnlistedAnimal)
                .GroupBy(f => f.ActualAnimal!)
                .Select(g => new { Animal = g.Key, Count = g.Count() })
                .OrderByDescending(a => a.Count)
                .ThenBy(a => a.Animal)
                .Take(count)
                .ToListAsync(cancellationToken);

            return animals.Select(a => (a.Animal, a.Count)).ToList();
        }
    }
}
