namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;
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
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (feedback, totalCount);
        }

        public Task<RecognitionFeedback?> FindForReviewAsync(int id) =>
            ForReview.FirstOrDefaultAsync(f => f.Id == id);
    }
}
