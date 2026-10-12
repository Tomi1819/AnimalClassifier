namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Recognitions.Statistics;
    using AnimalClassifier.Core.Recognitions.Statistics.Models;
    using AnimalClassifier.Tests.Support;
    using System.Net;
    using System.Net.Http.Json;

    public class StatisticsControllerTests : ApiTest
    {
        private const string TotalPath = "/api/statistics/total";
        private const string TopAnimalsPath = "/api/statistics/top-animal";
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
        public async Task GetTotal_CountsTheUsersRecognitions_ClearedOnesToo()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);

            await AddRecognitionAsync(account.UserId);
            await AddRecognitionAsync(account.UserId, isCleared: true);

            Assert.Equal(2, await user.GetFromJsonAsync<int>(TotalPath));
        }

        [Fact]
        public async Task GetTotal_LeavesOutOtherUsersRecognitions()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            await AddRecognitionAsync((await RegisterAsync()).UserId);

            Assert.Equal(0, await user.GetFromJsonAsync<int>(TotalPath));
        }

        // Another user's recognitions would put the cat first.
        [Fact]
        public async Task GetTopAnimals_ListsTheUsersAnimalRecognisedMostFirst()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddRecognitionAsync(account.UserId, "Dog");
            await AddRecognitionAsync(account.UserId, "Dog");
            await AddRecognitionAsync(account.UserId, "Cat");

            var otherUserId = (await RegisterAsync()).UserId;
            for (var i = 0; i < 3; i++)
            {
                await AddRecognitionAsync(otherUserId, "Cat");
            }

            var topAnimals = await user.GetFromJsonAsync<List<MostCommonAnimal>>(TopAnimalsPath);

            Assert.Equal([("Dog", 2), ("Cat", 1)], topAnimals!.Select(animal => (animal.AnimalName, animal.Count)).ToArray());
        }

        [Fact]
        public async Task GetTopAnimals_ListsOnlyTheLeaders()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            foreach (var animalName in new[] { "Cat", "Dog", "Fox", "Owl" })
            {
                await AddRecognitionAsync(account.UserId, animalName);
            }

            var topAnimals = await user.GetFromJsonAsync<List<MostCommonAnimal>>(TopAnimalsPath);

            Assert.Equal(StatisticsService.MostCommonAnimalCount, topAnimals!.Count);
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

            await AddRecognitionAsync(account.UserId, dateRecognized: dateRecognized);

            var activity = await GetActivityAsync(user);
            Assert.Equal(1, activity[day]);
            Assert.Equal(1, activity.Values.Sum());
        }

        [Fact]
        public async Task GetActivity_LeavesOutOtherUsersRecognitions()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            await AddRecognitionAsync((await RegisterAsync()).UserId);

            var activity = await GetActivityAsync(user);
            Assert.All(activity.Values, count => Assert.Equal(0, count));
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

            await AddRecognitionAsync(account.UserId, dateRecognized: dateRecognized);

            var activity = await GetActivityAsync(user, TokyoTimeZone);
            Assert.Equal(1, activity[tokyoDay]);
            Assert.Equal(0, activity[utcDay]);
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
