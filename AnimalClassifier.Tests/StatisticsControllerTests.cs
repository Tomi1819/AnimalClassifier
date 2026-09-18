namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;

    public class StatisticsControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string ActivityPath = "/api/statistics/activity";
        private const int Days = 7;

        // Nine hours ahead of UTC all year round, so a recognition late in the
        // UTC day falls on the next day there.
        private const string TokyoTimeZone = "Asia/Tokyo";

        private readonly ApiFactory factory;

        public StatisticsControllerTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task GetActivity_WithoutToken_ReturnsUnauthorized()
        {
            var response = await factory.CreateClient().GetAsync(ActivityPath);

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

            await AddRecognitionLogAsync(account.UserId, dateRecognized);

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

            await AddRecognitionLogAsync(account.UserId, dateRecognized);

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

        private async Task AddRecognitionLogAsync(string userId, DateTime dateRecognized)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            context.AnimalRecognitionLogs.Add(new AnimalRecognitionLog
            {
                AnimalName = "cat",
                ImagePath = $"/uploads/{Guid.NewGuid():N}.jpg",
                DateRecognized = dateRecognized,
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        private async Task<RegisterResponse> RegisterAsync()
        {
            var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
            {
                FullName = "Test User",
                Email = $"{Guid.NewGuid():N}@example.test",
                Password = Password
            });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
        }

        private async Task<HttpClient> SignInAsync(string email)
        {
            var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LogInRequest { Email = email, Password = Password });
            response.EnsureSuccessStatusCode();
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
            return client;
        }
    }
}
