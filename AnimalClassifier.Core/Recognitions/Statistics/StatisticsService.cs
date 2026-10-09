namespace AnimalClassifier.Core.Recognitions.Statistics
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Statistics.Models;
    using static AnimalClassifier.Core.Recognitions.Statistics.StatisticsMessages;

    // Everything is counted by the database, so that the work and what is
    // read do not grow with every recognition ever made. Only the dates of
    // the days asked for are read, to be put into their days in the caller's
    // time zone, which the database does not know.
    public class StatisticsService : IStatisticsService
    {
        /// <summary>
        /// How many of the animals recognised most are listed.
        /// </summary>
        public const int MostCommonAnimalCount = 3;

        private readonly IRecognitionStatisticsQueries recognitionStatistics;
        private readonly TimeProvider timeProvider;

        public StatisticsService(IRecognitionStatisticsQueries recognitionStatistics, TimeProvider timeProvider)
        {
            this.recognitionStatistics = recognitionStatistics;
            this.timeProvider = timeProvider;
        }

        public Task<int> GetTotalRecognitionsAsync(CancellationToken cancellationToken) =>
            recognitionStatistics.CountAsync(cancellationToken);

        public Task<int> GetUserCountAsync(CancellationToken cancellationToken) =>
            recognitionStatistics.CountUsersAsync(cancellationToken);

        public async Task<IReadOnlyList<MostCommonAnimal>> GetMostCommonAnimalsAsync(CancellationToken cancellationToken)
        {
            var animals = await recognitionStatistics.GetMostRecognisedAnimalsAsync(MostCommonAnimalCount, cancellationToken);

            return animals
                .Select(animal => new MostCommonAnimal { AnimalName = animal.AnimalName, Count = animal.Count })
                .ToList();
        }

        public async Task<IReadOnlyList<DailyRecognitionCount>> GetDailyRecognitionCountsAsync(int days, string? timeZoneId, CancellationToken cancellationToken)
        {
            var timeZone = FindTimeZone(timeZoneId);

            // Read once, so that today and the days read are the same
            // whenever the request runs across midnight.
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, timeZone));
            var firstDay = today.AddDays(1 - days);

            // A day more than asked for, which covers the first day wherever
            // it starts in relation to UTC.
            var dates = await recognitionStatistics.GetDatesSinceAsync(now.AddDays(-(days + 1)), cancellationToken);

            var counts = dates
                .GroupBy(date => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(date, timeZone)))
                .ToDictionary(day => day.Key, day => day.Count());

            return Enumerable.Range(0, days)
                .Select(offset => firstDay.AddDays(offset))
                .Select(day => new DailyRecognitionCount
                {
                    Date = day,
                    Count = counts.GetValueOrDefault(day)
                })
                .ToList();
        }

        private static TimeZoneInfo FindTimeZone(string? timeZoneId)
        {
            if (timeZoneId is null)
            {
                return TimeZoneInfo.Utc;
            }

            return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone)
                ? timeZone
                : throw new RequestRefusedException(UnknownTimeZone);
        }
    }
}
