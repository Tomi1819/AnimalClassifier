namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using Microsoft.EntityFrameworkCore;

    public class RecognitionFeedbackRepository : IRecognitionFeedbackRepository
    {
        private readonly AnimalClassifierDbContext context;

        public RecognitionFeedbackRepository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        // Feedback whose user did not allow training is never reviewed, so
        // the review never sees it.
        private IQueryable<RecognitionFeedback> ForReview =>
            context.RecognitionFeedback.Where(f => f.AllowsTraining);

        public void Add(RecognitionFeedback feedback) => context.RecognitionFeedback.Add(feedback);

        public void Remove(RecognitionFeedback feedback) => context.RecognitionFeedback.Remove(feedback);

        public Task<RecognitionFeedback?> FindForUserAsync(string userId, int recognitionId) =>
            context.RecognitionFeedback
                .FirstOrDefaultAsync(f => f.RecognitionId == recognitionId && f.Recognition.UserId == userId);

        public async Task<(IReadOnlyList<RecognitionFeedback> Feedback, int TotalCount)> GetPageForUserAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var given = context.RecognitionFeedback.Where(f => f.Recognition.UserId == userId);

            var totalCount = await given.CountAsync(cancellationToken);
            var feedback = await given
                .AsNoTracking()
                .Include(f => f.Recognition)
                .OrderByDescending(f => f.DateSubmitted)
                .ThenByDescending(f => f.Id)
                .TakePage(page, pageSize)
                .ToListAsync(cancellationToken);

            return (feedback, totalCount);
        }

        public async Task<(IReadOnlyList<RecognitionFeedback> Feedback, int TotalCount)> GetPageForReviewAsync(FeedbackReviewStatus status, int page, int pageSize, CancellationToken cancellationToken)
        {
            var inReview = ForReview.Where(f => f.ReviewStatus == status);

            var totalCount = await inReview.CountAsync(cancellationToken);
            var feedback = await inReview
                .AsNoTracking()
                .Include(f => f.Recognition)
                .OrderBy(f => f.DateSubmitted)
                .ThenBy(f => f.Id)
                .TakePage(page, pageSize)
                .ToListAsync(cancellationToken);

            return (feedback, totalCount);
        }

        public Task<RecognitionFeedback?> FindForReviewAsync(int id) =>
            ForReview.FirstOrDefaultAsync(f => f.Id == id);

        public async Task<IReadOnlyList<RecognitionFeedback>> GetAcceptedForTrainingAsync(CancellationToken cancellationToken) =>
            await ForReview
                .Where(f => f.ReviewStatus == FeedbackReviewStatus.Accepted)
                .AsNoTracking()
                .Include(f => f.Recognition)
                .OrderBy(f => f.DateSubmitted)
                .ThenBy(f => f.Id)
                .ToListAsync(cancellationToken);

        public Task<Dictionary<FeedbackVerdict, int>> CountByVerdictAsync(CancellationToken cancellationToken) =>
            context.RecognitionFeedback
                .GroupBy(f => f.Verdict)
                .Select(g => new { Verdict = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Verdict, g => g.Count, cancellationToken);

        public Task<Dictionary<FeedbackReviewStatus, int>> CountForReviewByStatusAsync(CancellationToken cancellationToken) =>
            ForReview
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
