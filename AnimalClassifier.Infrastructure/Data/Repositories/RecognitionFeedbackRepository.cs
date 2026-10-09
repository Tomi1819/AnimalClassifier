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
            var inReview = context.RecognitionFeedback.ForReview().Where(f => f.ReviewStatus == status);

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
            context.RecognitionFeedback.ForReview().FirstOrDefaultAsync(f => f.Id == id);

        public async Task<IReadOnlyList<RecognitionFeedback>> GetAcceptedForTrainingAsync(CancellationToken cancellationToken) =>
            await context.RecognitionFeedback.ForReview()
                .Where(f => f.ReviewStatus == FeedbackReviewStatus.Accepted)
                .AsNoTracking()
                .Include(f => f.Recognition)
                .OrderBy(f => f.DateSubmitted)
                .ThenBy(f => f.Id)
                .ToListAsync(cancellationToken);
    }
}
