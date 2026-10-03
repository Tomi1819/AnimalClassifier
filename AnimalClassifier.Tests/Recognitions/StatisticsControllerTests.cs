namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Recognitions.Statistics;
    using AnimalClassifier.Core.Recognitions.Statistics.Models;
    using AnimalClassifier.Tests.Support;
    using System.Net;
    using System.Net.Http.Json;

    public class StatisticsControllerTests : ApiTest
    {
        private const string ActivityPath = "/api/statistics/activity";
        private const int Days = 7;

        // Nine hours ahead of UTC all year round, so a recognition late in the
        // UTC day falls on the next day there.
        private const string TokyoTimeZone = "Asia/Tokyo";

        public StatisticsControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        // A cleared recognition was still made, so it is still counted.
        [Fact]
        public async Task GetTotal_CountsEveryRecognition_ClearedOnesToo()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var before = await user.GetFromJsonAsync<int>("/api/statistics/total");

            await AddRecognitionAsync(account.UserId);
            await AddRecognitionAsync(account.UserId, isCleared: true);

            Assert.Equal(before + 2, await user.GetFromJsonAsync<int>("/api/statistics/total"));
        }

        [Fact]
        public async Task GetUniqueUsers_CountsEachUserOnce()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var before = await user.GetFromJsonAsync<int>("/api/statistics/users");

            await AddRecognitionAsync(account.UserId);
            await AddRecognitionAsync(account.UserId);

            Assert.Equal(before + 1, await user.GetFromJsonAsync<int>("/api/statistics/users"));
        }

        [Fact]
        public async Task GetTopAnimals_ListsTheAnimalRecognisedMostFirst()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var animalName = $"animal{Guid.NewGuid():N}";
            for (var i = 0; i < 10; i++)
            {
                await AddRecognitionAsync(account.UserId, animalName);
            }

            var topAnimals = await user.GetFromJsonAsync<List<MostCommonAnimal>>("/api/statistics/top-animal");

            Assert.Equal(animalName, topAnimals![0].AnimalName);
            Assert.Equal(10, topAnimals[0].Count);
            Assert.True(topAnimals.Count <= StatisticsService.MostCommonAnimalCount);
        }

        [Fact]
        public async Task GetActivity_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().GetAsync(ActivityPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_ReturnsEveryDayUpToToday()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var activity = await user.GetFromJsonAsync<List<DailyRecognitionCount>>($"{ActivityPath}?days={Days}");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var expectedDays = Enumerable.Range(0, Days).Select(offset => today.AddDays(offset - Days + 1));
            Assert.Equal(expectedDays, activity!.Select(day => day.Date));
        }

        [Fact]
        public async Task GetActivity_CountsRecognitionOnItsDay()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var dateRecognized = DateTime.UtcNow.AddDays(-2);
            var day = DateOnly.FromDateTime(dateRecognized);
            var before = await GetActivityAsync(user);

            await AddRecognitionAsync(account.UserId, dateRecognized: dateRecognized);

            var after = await GetActivityAsync(user);
            Assert.Equal(before[day] + 1, after[day]);
        }

        [Fact]
        public async Task GetActivity_InTimeZone_CountsRecognitionOnLocalDay()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            // 22:00 UTC is 07:00 the next morning in Tokyo.
            var dateRecognized = DateTime.UtcNow.Date.AddDays(-3).AddHours(22);
            var utcDay = DateOnly.FromDateTime(dateRecognized);
            var tokyoDay = utcDay.AddDays(1);
            var before = await GetActivityAsync(user, TokyoTimeZone);

            await AddRecognitionAsync(account.UserId, dateRecognized: dateRecognized);

            var after = await GetActivityAsync(user, TokyoTimeZone);
            Assert.Equal(before[tokyoDay] + 1, after[tokyoDay]);
            Assert.Equal(before[utcDay], after[utcDay]);
        }

        [Fact]
        public async Task GetActivity_UnknownTimeZone_ReturnsBadRequest()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{ActivityPath}?timeZone=Nowhere/Nothing");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_MoreThanAYear_ReturnsBadRequest()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{ActivityPath}?days=366");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private static async Task<Dictionary<DateOnly, int>> GetActivityAsync(HttpClient client, string? timeZone = null)
        {
            var path = timeZone is null
                ? $"{ActivityPath}?days={Days}"
                : $"{ActivityPath}?days={Days}&timeZone={Uri.EscapeDataString(timeZone)}";

            var activity = await client.GetFromJsonAsync<List<DailyRecognitionCount>>(path);
            return activity!.ToDictionary(day => day.Date, day => day.Count);
        }
    }
}
