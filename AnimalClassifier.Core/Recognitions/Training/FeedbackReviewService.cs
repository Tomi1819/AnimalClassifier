namespace AnimalClassifier.Core.Recognitions.Training
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Core.Recognitions.Training.Models;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using static AnimalClassifier.Core.Recognitions.Training.TrainingMessages;

    public class FeedbackReviewService : IFeedbackReviewService
    {
        private const int PageSize = 20;

        private readonly IRecognitionFeedbackRepository feedbackRepository;
        private readonly IMediaLinkService mediaLinks;
        private readonly IUnitOfWork unitOfWork;

        public FeedbackReviewService(IRecognitionFeedbackRepository feedbackRepository,
                                     IMediaLinkService mediaLinks,
                                     IUnitOfWork unitOfWork)
        {
            this.feedbackRepository = feedbackRepository;
            this.mediaLinks = mediaLinks;
            this.unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<FeedbackReviewItem>> GetFeedbackAsync(FeedbackReviewStatus status, int page, CancellationToken cancellationToken)
        {
            var (feedback, totalCount) = await feedbackRepository.GetPageForReviewAsync(status, page, PageSize, cancellationToken);

            return new PagedResult<FeedbackReviewItem>
            {
                Items = feedback.Select(ToReviewItem).ToList(),
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount
            };
        }

        public Task AcceptAsync(int feedbackId) =>
            ReviewAsync(feedbackId, FeedbackReviewStatus.Accepted, FeedbackAlreadyAccepted);

        public Task RejectAsync(int feedbackId) =>
            ReviewAsync(feedbackId, FeedbackReviewStatus.Rejected, FeedbackAlreadyRejected);

        private async Task ReviewAsync(int feedbackId, FeedbackReviewStatus status, string unchanged)
        {
            var feedback = await feedbackRepository.FindForReviewAsync(feedbackId)
                ?? throw new NotFoundException(FeedbackNotFound);

            if (feedback.ReviewStatus == status)
            {
                throw new RequestRefusedException(unchanged);
            }

            feedback.ReviewStatus = status;
            feedback.DateReviewed = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();
        }

        private FeedbackReviewItem ToReviewItem(RecognitionFeedback feedback) => new()
        {
            Id = feedback.Id,
            ImagePath = mediaLinks.CreateLink(feedback.Recognition.UserId, feedback.Recognition.FileName),
            RecognizedAnimal = feedback.Recognition.AnimalName,
            PredictionScore = feedback.Recognition.PredictionScore,
            Verdict = feedback.Verdict,
            Label = TrainingLabel.For(feedback),
            IsKnownAnimal = TrainingLabel.IsKnownToTheModel(feedback),
            Comment = feedback.Comment,
            ReviewStatus = feedback.ReviewStatus,
            DateSubmitted = feedback.DateSubmitted,
            DateReviewed = feedback.DateReviewed
        };
    }
}
