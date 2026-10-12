namespace AnimalClassifier.Core.Data.Queries
{
    /// <summary>
    /// The statistics page's counts, read across one user's recognitions,
    /// cleared ones included.
    /// </summary>
    public interface IRecognitionStatisticsQueries
    {
        /// <summary>
        /// How many recognitions the user has made.
        /// </summary>
        Task<int> CountAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// The animals the user recognised most, with how often each was, most
        /// first.
        /// </summary>
        /// <param name="count">How many animals to read.</param>
        Task<IReadOnlyList<(string AnimalName, int Count)>> GetMostRecognisedAnimalsAsync(string userId, int count, CancellationToken cancellationToken);

        /// <summary>
        /// When each recognition the user made since the date was made, and
        /// nothing else about them.
        /// </summary>
        Task<IReadOnlyList<DateTime>> GetDatesSinceAsync(string userId, DateTime since, CancellationToken cancellationToken);
    }
}
