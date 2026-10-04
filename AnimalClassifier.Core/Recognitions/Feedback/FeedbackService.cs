namespace AnimalClassifier.Core.Recognitions.Feedback
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using static AnimalClassifier.Core.Recognitions.Feedback.FeedbackMessages;

    public class FeedbackService : IFeedbackService
    {
        private readonly IImageClassifier classifier;
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IRecognitionFeedbackRepository feedbackRepository;
        private readonly IUnitOfWork unitOfWork;

        public FeedbackService(IImageClassifier classifier,
                               IRecognitionLogRepository recognitionLogs,
                               IRecognitionFeedbackRepository feedbackRepository,
                               IUnitOfWork unitOfWork)
        {
            this.classifier = classifier;
            this.recognitionLogs = recognitionLogs;
            this.feedbackRepository = feedbackRepository;
            this.unitOfWork = unitOfWork;
        }

        public IReadOnlyList<string> GetKnownAnimals() => classifier.KnownAnimals;

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
            feedback.DateSubmitted = DateTime.UtcNow;

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
    }
}
