namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;

    public class AnimalControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string SearchPath = "/api/animal/search";

        private readonly ApiFactory factory;

        public AnimalControllerTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task Search_AnimalNeverRecognised_ReturnsNotFound()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{SearchPath}?searchTerm=animal{Guid.NewGuid():N}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Search_IgnoresCase()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var animalName = $"animal{Guid.NewGuid():N}";
            await AddRecognitionLogAsync(account.UserId, animalName);

            var results = await user.GetFromJsonAsync<List<AnimalSearchResult>>($"{SearchPath}?searchTerm={animalName.ToUpperInvariant()}");

            var result = Assert.Single(results!);
            Assert.Equal(animalName, result.AnimalName);
        }

        private async Task AddRecognitionLogAsync(string userId, string animalName)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            context.AnimalRecognitionLogs.Add(new AnimalRecognitionLog
            {
                AnimalName = animalName,
                ImagePath = $"/uploads/{Guid.NewGuid():N}.jpg",
                DateRecognized = DateTime.UtcNow,
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
