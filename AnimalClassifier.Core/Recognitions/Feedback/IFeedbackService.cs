namespace AnimalClassifier.Core.Recognitions.Feedback
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;

    /// <summary>
    /// What users say of the animals recognised in their images: whether the
    /// model was right, and which animal it was when it was not. Each image's
    /// recognition can have one feedback, which its owner can change or
    /// withdraw. One that allows training waits for an administrator's review
    /// before the model is trained on it, and changing it has it wait again.
    /// </summary>
    public interface IFeedbackService
    {
        /// <summary>
        /// The animals the model knows, which a correction names one of,
        /// alphabetically.
        /// </summary>
        IReadOnlyList<string> GetKnownAnimals();

        /// <summary>
        /// One page of the feedback the user has given, most recently given
        /// first, including on recognitions they have cleared from their
        /// history, so that they can still withdraw it.
        /// </summary>
        Task<PagedResult<FeedbackItem>> GetFeedbackAsync(string userId, int page, CancellationToken cancellationToken);

        /// <summary>
        /// Gives feedback on one of the user's recognitions, in place of any
        /// they gave before.
        /// </summary>
        /// <returns>The feedback as it is kept.</returns>
        /// <exception cref="NotFoundException">
        /// When the recognition does not exist or is not the user's.
        /// </exception>
        /// <exception cref="RequestRefusedException">
        /// When the recognition is a video's, or the feedback does not say
        /// enough; see <see cref="FeedbackValidator"/>.
        /// </exception>
        Task<FeedbackDetails> GiveFeedbackAsync(string userId, int recognitionId, FeedbackRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Withdraws the feedback on one of the user's recognitions. A model
        /// already trained on it keeps what it learnt, but none trained after
        /// will be.
        /// </summary>
        /// <exception cref="NotFoundException">
        /// When there is no feedback on it, or it is not the user's.
        /// </exception>
        Task WithdrawFeedbackAsync(string userId, int recognitionId);
    }
}
