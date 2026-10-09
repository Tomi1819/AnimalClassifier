namespace AnimalClassifier.Core.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// The counts that sum up every user's feedback for the administrator.
    /// </summary>
    public interface IFeedbackSummaryQueries
    {
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
