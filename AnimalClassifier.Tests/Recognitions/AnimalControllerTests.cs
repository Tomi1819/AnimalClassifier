namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Tests.Support;
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
            await AddRecognitionAsync(account.UserId, animalName);

            var results = await user.GetFromJsonAsync<List<AnimalSearchResult>>($"{SearchPath}?searchTerm={animalName.ToUpperInvariant()}");

            var result = Assert.Single(results!);
            Assert.Equal(animalName, result.AnimalName);
        }
    }
}
