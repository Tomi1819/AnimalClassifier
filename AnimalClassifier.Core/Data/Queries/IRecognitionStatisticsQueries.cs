namespace AnimalClassifier.Core.Data.Queries
{
    /// <summary>
    /// The statistics page's counts, read across every user's recognitions,
    /// cleared ones included.
    /// </summary>
    public interface IRecognitionStatisticsQueries
    {
        /// <summary>
        /// How many recognitions have been made.
        /// </summary>
        Task<int> CountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// How many users have made a recognition, cleared ones included.
        /// </summary>
        Task<int> CountUsersAsync(CancellationToken cancellationToken);

        /// <summary>
        /// The animals recognised most, with how often each was, most first.
        /// </summary>
        /// <param name="count">How many animals to read.</param>
        Task<IReadOnlyList<(string AnimalName, int Count)>> GetMostRecognisedAnimalsAsync(int count, CancellationToken cancellationToken);

        /// <summary>
        /// When each recognition since the date was made, and nothing else
        /// about them.
        /// </summary>
        Task<IReadOnlyList<DateTime>> GetDatesSinceAsync(DateTime since, CancellationToken cancellationToken);
    }
}
