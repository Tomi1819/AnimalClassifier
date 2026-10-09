namespace AnimalClassifier.Core.Recognitions.Feedback
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using static AnimalClassifier.Core.Recognitions.Feedback.FeedbackMessages;

    public class FeedbackService : IFeedbackService
    {
        private const int PageSize = 20;

        private readonly IImageClassifier classifier;
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IRecognitionFeedbackRepository feedbackRepository;
        private readonly IMediaLinkService mediaLinks;
        private readonly IUnitOfWork unitOfWork;
        private readonly TimeProvider timeProvider;

        public FeedbackService(IImageClassifier classifier,
                               IRecognitionLogRepository recognitionLogs,
                               IRecognitionFeedbackRepository feedbackRepository,
                               IMediaLinkService mediaLinks,
                               IUnitOfWork unitOfWork,
                               TimeProvider timeProvider)
        {
            this.classifier = classifier;
            this.recognitionLogs = recognitionLogs;
            this.feedbackRepository = feedbackRepository;
            this.mediaLinks = mediaLinks;
            this.unitOfWork = unitOfWork;
            this.timeProvider = timeProvider;
        }

        public IReadOnlyList<string> GetKnownAnimals() => classifier.KnownAnimals;

        public async Task<PagedResult<FeedbackItem>> GetFeedbackAsync(string userId, int page, CancellationToken cancellationToken)
        {
            var (feedback, totalCount) = await feedbackRepository.GetPageForUserAsync(userId, page, PageSize, cancellationToken);

            return new PagedResult<FeedbackItem>
            {
                Items = feedback.Select(ToFeedbackItem).ToList(),
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount
            };
        }

        // A caller who goes away stops it before anything is written.
        public async Task<FeedbackDetails> GiveFeedbackAsync(string userId, int recognitionId, FeedbackRequest request, CancellationToken cancellationToken)
        {
            var recognition = await recognitionLogs.GetForUserAsync(userId, recognitionId, cancellationToken)
                ?? throw new NotFoundException(RecognitionNotFound);

            // A video's recognition names only the animal seen in it most, so
            // it cannot say which frames were wrong, and there is no image of
            // its own to train on.
            if (!MediaFile.IsImage(recognition.FileName))
            {
                throw new RequestRefusedException(ImagesOnly);
            }

            var actualAnimal = FeedbackValidator.TidyActualAnimal(request.Verdict, request.ActualAnimal, recognition.AnimalName, classifier.KnownAnimals);
            var comment = FeedbackValidator.TidyComment(request.Comment);

            var feedback = await feedbackRepository.FindForUserAsync(userId, recognitionId);

            if (feedback is null)
            {
                feedback = new RecognitionFeedback { RecognitionId = recognitionId };
                feedbackRepository.Add(feedback);
            }

            feedback.Verdict = request.Verdict;
            feedback.ActualAnimal = actualAnimal;
            feedback.Comment = comment;
            feedback.AllowsTraining = request.AllowsTraining;
            feedback.DateSubmitted = timeProvider.GetUtcNow().UtcDateTime;

            // A review was of what the feedback said then, so a changed one
            // waits for another.
            feedback.ReviewStatus = FeedbackReviewStatus.Pending;
            feedback.DateReviewed = null;

            await unitOfWork.SaveChangesAsync();

            return FeedbackDetails.From(feedback);
        }

        public async Task WithdrawFeedbackAsync(string userId, int recognitionId)
        {
            var feedback = await feedbackRepository.FindForUserAsync(userId, recognitionId)
                ?? throw new NotFoundException(FeedbackNotFound);

            feedbackRepository.Remove(feedback);
            await unitOfWork.SaveChangesAsync();
        }

        private FeedbackItem ToFeedbackItem(RecognitionFeedback feedback) => new()
        {
            RecognitionId = feedback.RecognitionId,
            ImagePath = mediaLinks.CreateLink(feedback.Recognition.UserId, feedback.Recognition.FileName),
            RecognizedAnimal = feedback.Recognition.AnimalName,
            PredictionScore = feedback.Recognition.PredictionScore,
            DateRecognized = feedback.Recognition.DateRecognized,
            Feedback = FeedbackDetails.From(feedback)
        };
    }
}
