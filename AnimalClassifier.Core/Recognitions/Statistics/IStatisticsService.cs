namespace AnimalClassifier.Core.Recognitions.Statistics
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Statistics.Models;

    /// <summary>
    /// Figures about every recognition made, by every user. A recognition
    /// cleared from its owner's history still counts, since it was still
    /// made.
    /// </summary>
    public interface IStatisticsService
    {
        Task<int> GetTotalRecognitionsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// How many users have made a recognition.
        /// </summary>
        Task<int> GetUserCountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// The <see cref="StatisticsService.MostCommonAnimalCount"/> animals
        /// recognised most, most first.
        /// </summary>
        Task<IReadOnlyList<MostCommonAnimal>> GetMostCommonAnimalsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// How many recognitions were made on each of the last days, up to
        /// and including today, where a day starts and ends as it does in the
        /// time zone. A day without any is counted as none.
        /// </summary>
        /// <param name="timeZoneId">
        /// The time zone's id, such as <c>Europe/Sofia</c>, or null for UTC.
        /// </param>
        /// <exception cref="RequestRefusedException">
        /// When the server does not know the time zone.
        /// </exception>
        Task<IReadOnlyList<DailyRecognitionCount>> GetDailyRecognitionCountsAsync(int days, string? timeZoneId, CancellationToken cancellationToken);
    }
}
