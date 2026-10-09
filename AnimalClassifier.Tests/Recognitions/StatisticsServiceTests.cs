namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Statistics;
    using Microsoft.Extensions.Time.Testing;

    /// <summary>
    /// The daily activity against a clock the test sets, which the API tests,
    /// running on the real one, cannot do near a day's end.
    /// </summary>
    public class StatisticsServiceTests
    {
        private const string TokyoTimeZone = "Asia/Tokyo";

        // 23:30 in UTC, when it is already 08:30 the next morning in Tokyo.
        private static readonly DateTimeOffset LateEvening = new(2026, 3, 1, 23, 30, 0, TimeSpan.Zero);

        [Fact]
        public async Task DailyCounts_EndOnTheCallersToday()
        {
            var service = Service([]);

            var utc = await service.GetDailyRecognitionCountsAsync(2, timeZoneId: null, CancellationToken.None);
            var tokyo = await service.GetDailyRecognitionCountsAsync(2, TokyoTimeZone, CancellationToken.None);

            Assert.Equal(new DateOnly(2026, 3, 1), utc[^1].Date);
            Assert.Equal(new DateOnly(2026, 3, 2), tokyo[^1].Date);
        }

        // 14:00 and 16:00 in UTC fall either side of midnight in Tokyo.
        [Fact]
        public async Task DailyCounts_PutEachRecognitionOnTheCallersDay()
        {
            var service = Service([OnFirstOfMarch(hour: 14), OnFirstOfMarch(hour: 16)]);

            var utc = await service.GetDailyRecognitionCountsAsync(2, timeZoneId: null, CancellationToken.None);
            var tokyo = await service.GetDailyRecognitionCountsAsync(2, TokyoTimeZone, CancellationToken.None);

            Assert.Equal([0, 2], utc.Select(day => day.Count).ToArray());
            Assert.Equal([1, 1], tokyo.Select(day => day.Count).ToArray());
        }

        private static StatisticsService Service(IReadOnlyList<DateTime> dates) =>
            new(new StubStatisticsQueries(dates), new FakeTimeProvider(LateEvening));

        private static DateTime OnFirstOfMarch(int hour) => new(2026, 3, 1, hour, 0, 0, DateTimeKind.Utc);

        // The daily counts read nothing but the dates.
        private sealed class StubStatisticsQueries : IRecognitionStatisticsQueries
        {
            private readonly IReadOnlyList<DateTime> dates;

            public StubStatisticsQueries(IReadOnlyList<DateTime> dates)
            {
                this.dates = dates;
            }

            public Task<IReadOnlyList<DateTime>> GetDatesSinceAsync(DateTime since, CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyList<DateTime>>(dates.Where(date => date >= since).ToList());

            public Task<int> CountAsync(CancellationToken cancellationToken) =>
                throw new NotSupportedException();

            public Task<int> CountUsersAsync(CancellationToken cancellationToken) =>
                throw new NotSupportedException();

            public Task<IReadOnlyList<(string AnimalName, int Count)>> GetMostRecognisedAnimalsAsync(int count, CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }
    }
}
