namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Json;

    public class AnimalControllerTests : ApiTest
    {
        private const string SearchPath = "/api/animal/search";

        public AnimalControllerTests(ApiFactory factory)
            : base(factory)
        {
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
            using var scope = Factory.Services.CreateScope();
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
    }
}
