namespace AnimalClassifier.Core.Recognitions.Training
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Recognitions.Training.Models;

    /// <summary>
    /// An administrator's check of the feedback users offer for training,
    /// since a user can be mistaken about an animal as well as the model.
    /// Only feedback its user allowed training on is reviewed, and only an
    /// accepted one is exported to train on. A user who changes their
    /// feedback has it reviewed again.
    /// </summary>
    public interface IFeedbackReviewService
    {
        /// <summary>
        /// One page of the feedback in one state of review, in the order it
        /// was given, so that none waits behind newer feedback.
        /// </summary>
        Task<PagedResult<FeedbackReviewItem>> GetFeedbackAsync(FeedbackReviewStatus status, int page, CancellationToken cancellationToken);

        /// <summary>
        /// Accepts a feedback for training, whatever its review said before.
        /// </summary>
        /// <exception cref="NotFoundException">
        /// When the feedback does not exist or does not allow training.
        /// </exception>
        /// <exception cref="RequestRefusedException">When it is accepted already.</exception>
        Task AcceptAsync(int feedbackId);

        /// <summary>
        /// Keeps a feedback out of training, whatever its review said before.
        /// </summary>
        /// <exception cref="NotFoundException">
        /// When the feedback does not exist or does not allow training.
        /// </exception>
        /// <exception cref="RequestRefusedException">When it is rejected already.</exception>
        Task RejectAsync(int feedbackId);
    }
}
