namespace AnimalClassifier.Core.Data.Queries
{
    /// <summary>
    /// What a user's achievements are worked out from. They are not stored:
    /// each is read again from the recognitions, so that one earned before
    /// achievements existed counts as well, and a recognition deleted takes
    /// what it earned with it.
    /// </summary>
    public interface IRecognitionAchievementQueries
    {
        /// <summary>
        /// Every one of the user's recognitions, cleared ones and videos
        /// included, as the collection counts them, in the order they were
        /// made: by when, and then by the order they were recorded in.
        /// </summary>
        Task<IReadOnlyList<RecognitionFacts>> GetFactsAsync(string userId, float minimumScore, CancellationToken cancellationToken);
    }
}
