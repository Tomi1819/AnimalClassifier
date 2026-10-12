namespace AnimalClassifier.Core.Recognitions.Statistics
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Statistics.Models;

    /// <summary>
    /// Figures about one user's own recognitions, which no other user's
    /// change. A recognition cleared from the history still counts, since it
    /// was still made.
    /// </summary>
    public interface IStatisticsService
    {
        Task<int> GetTotalRecognitionsAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// The <see cref="StatisticsService.MostCommonAnimalCount"/> animals
        /// the user recognised most, most first.
        /// </summary>
        Task<IReadOnlyList<MostCommonAnimal>> GetMostCommonAnimalsAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// How many recognitions the user made on each of the last days, up to
        /// and including today, where a day starts and ends as it does in the
        /// time zone. A day without any is counted as none.
        /// </summary>
        /// <param name="timeZoneId">
        /// The time zone's id, such as <c>Europe/Sofia</c>, or null for UTC.
        /// </param>
        /// <exception cref="RequestRefusedException">
        /// When the server does not know the time zone.
        /// </exception>
        Task<IReadOnlyList<DailyRecognitionCount>> GetDailyRecognitionCountsAsync(string userId, int days, string? timeZoneId, CancellationToken cancellationToken);
    }
}
