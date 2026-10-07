namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Recognitions.Search;
    using AnimalClassifier.Core.Recognitions.Search.Models;
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

        [Fact]
        public async Task Search_WithoutATerm_ReturnsBadRequest()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{SearchPath}?searchTerm=%20");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(SearchMessages.EnterSearchTerm, (await response.Content.ReadFromJsonAsync<MessageResponse>())!.Message);
        }

        // The page shows images, and a video has none to show.
        [Fact]
        public async Task Search_LeavesOutVideos()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var animalName = $"animal{Guid.NewGuid():N}";
            await AddRecognitionAsync(account.UserId, animalName, fileName: "clip.mp4");

            var response = await user.GetAsync($"{SearchPath}?searchTerm={animalName}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Its owner put it away, so it is not shown to everyone else.
        [Fact]
        public async Task Search_LeavesOutClearedRecognitions()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var animalName = $"animal{Guid.NewGuid():N}";
            await AddRecognitionAsync(account.UserId, animalName, isCleared: true);

            var response = await user.GetAsync($"{SearchPath}?searchTerm={animalName}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Search_LeavesOutMatchesRecognisedFarLessOften()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var term = $"animal{Guid.NewGuid():N}";
            for (var i = 0; i < 3; i++)
            {
                await AddRecognitionAsync(account.UserId, $"{term}-often");
            }
            await AddRecognitionAsync(account.UserId, $"{term}-rarely");

            var results = await user.GetFromJsonAsync<List<AnimalSearchResult>>($"{SearchPath}?searchTerm={term}");

            var result = Assert.Single(results!);
            Assert.Equal($"{term}-often", result.AnimalName);
            Assert.Equal(3, result.ImagePaths.Count);
        }

        // Every image is counted, but only so many are linked to, and a video
        // is neither.
        [Fact]
        public async Task Search_LinksOnlyTheMostRecentImages_ButCountsThemAll()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var animalName = $"animal{Guid.NewGuid():N}";
            for (var i = 0; i <= AnimalSearchService.MaxImagesPerAnimal; i++)
            {
                await AddRecognitionAsync(account.UserId, animalName);
            }
            await AddRecognitionAsync(account.UserId, animalName, fileName: "clip.mp4");

            var results = await user.GetFromJsonAsync<List<AnimalSearchResult>>($"{SearchPath}?searchTerm={animalName}");

            var result = Assert.Single(results!);
            Assert.Equal(AnimalSearchService.MaxImagesPerAnimal + 1, result.Count);
            Assert.Equal(AnimalSearchService.MaxImagesPerAnimal, result.ImagePaths.Count);
        }
    }
}
