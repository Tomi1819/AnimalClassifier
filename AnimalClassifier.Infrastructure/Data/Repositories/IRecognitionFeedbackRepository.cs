namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;

    /// <summary>
    /// The feedback users give on their recognitions, at most one each. It is
    /// removed with its recognition, by the database, so nothing here removes
    /// a user's feedback in bulk.
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

        /// <summary>
        /// How much feedback says each verdict, leaving out the verdicts none
        /// says.
        /// </summary>
        Task<Dictionary<FeedbackVerdict, int>> CountByVerdictAsync(CancellationToken cancellationToken);

        /// <summary>
        /// How much of the feedback that allows training is in each state of
        /// review, leaving out the states none is in.
        /// </summary>
        Task<Dictionary<FeedbackReviewStatus, int>> CountForReviewByStatusAsync(CancellationToken cancellationToken);

        /// <summary>
        /// The animals the model named most often in place of another it
        /// knows, with that other and how often, most first.
        /// </summary>
        /// <param name="count">How many pairs to read.</param>
        Task<IReadOnlyList<(string RecognizedAnimal, string ActualAnimal, int Count)>> GetMostCommonMistakesAsync(int count, CancellationToken cancellationToken);

        /// <summary>
        /// The animals the model does not know that users named most, with how
        /// often, most first.
        /// </summary>
        /// <param name="count">How many animals to read.</param>
        Task<IReadOnlyList<(string Animal, int Count)>> GetMostNamedUnlistedAnimalsAsync(int count, CancellationToken cancellationToken);
    }
}
