namespace AnimalClassifier.Core.Data.Repositories
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// The feedback users give on their recognitions, at most one each. It is
    /// removed with its recognition, by the database, so nothing here removes
    /// a user's feedback in bulk.
    ///
    /// Summing it up for the administrator is IFeedbackSummaryQueries' to do.
    /// </summary>
    public interface IRecognitionFeedbackRepository
    {
        /// <summary>
        /// Adds a feedback, which is stored on the next save.
        /// </summary>
        void Add(RecognitionFeedback feedback);

        /// <summary>
        /// Removes a feedback, which goes on the next save.
        /// </summary>
        void Remove(RecognitionFeedback feedback);

        /// <summary>
        /// The feedback on one recognition, if there is any and the
        /// recognition belongs to the user. It is tracked, so a change made to
        /// it is stored on the next save.
        /// </summary>
        Task<RecognitionFeedback?> FindForUserAsync(string userId, int recognitionId);

        /// <summary>
        /// One page of the feedback a user has given, most recently given
        /// first, each with its recognition, cleared ones included, and how
        /// much they have given in all.
        /// </summary>
        Task<(IReadOnlyList<RecognitionFeedback> Feedback, int TotalCount)> GetPageForUserAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>
        /// One page of the feedback that allows training and is in one state
        /// of review, in the order it was given, each with its recognition,
        /// and how much there is in that state in all.
        /// </summary>
        Task<(IReadOnlyList<RecognitionFeedback> Feedback, int TotalCount)> GetPageForReviewAsync(FeedbackReviewStatus status, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>
        /// One feedback, if it exists and allows training. It is tracked, so a
        /// review of it is stored on the next save.
        /// </summary>
        Task<RecognitionFeedback?> FindForReviewAsync(int id);

        /// <summary>
        /// Every feedback that allows training and was accepted, each with its
        /// recognition, in the order it was given.
        /// </summary>
        Task<IReadOnlyList<RecognitionFeedback>> GetAcceptedForTrainingAsync(CancellationToken cancellationToken);
    }
}
