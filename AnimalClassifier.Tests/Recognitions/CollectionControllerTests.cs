namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Recognitions.Collection.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Infrastructure.Storage;
    using AnimalClassifier.Tests.Support;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Http.Json;

    public class CollectionControllerTests : ApiTest
    {
        private const string CollectionPath = "/api/collection";

        public CollectionControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetCollection_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().GetAsync(CollectionPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCollection_ListsEveryAnimalTheModelKnows_NoneFoundAtFirst()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var collection = await GetCollectionAsync(user);

            Assert.Equal(Factory.Classifier.KnownAnimals, collection.Animals.Select(animal => animal.AnimalName));
            Assert.Equal(Factory.Classifier.KnownAnimals.Count, collection.KnownCount);
            Assert.Equal(0, collection.FoundCount);
            Assert.All(collection.Animals, animal =>
            {
                Assert.Equal(0, animal.Count);
                Assert.Null(animal.DateFirstRecognized);
                Assert.Null(animal.ImagePath);
            });
        }

        [Fact]
        public async Task GetCollection_CountsAnimalFound_WithTheDateItWasFirstFound()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var firstFound = DateTime.UtcNow.AddDays(-5);
            await AddRecognitionAsync(account.UserId, "Fox", dateRecognized: DateTime.UtcNow.AddDays(-1));
            await AddRecognitionAsync(account.UserId, "Fox", dateRecognized: firstFound);

            var collection = await GetCollectionAsync(user);

            var fox = FindAnimal(collection, "Fox");
            Assert.Equal(1, collection.FoundCount);
            Assert.Equal(2, fox.Count);
            Assert.Equal(firstFound, fox.DateFirstRecognized);
        }

        // Clearing the history does not undo a recognition, and a video finds
        // an animal as well as an image does, but neither has an image to show.
        [Fact]
        public async Task GetCollection_CountsVideosAndClearedRecognitions_WithoutAnImage()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddImageAsync(account.UserId, "Wolf", predictionScore: 0.9f, isCleared: true);
            await AddRecognitionAsync(account.UserId, "Wolf", fileName: "clip.mp4", predictionScore: 0.8f);

            var collection = await GetCollectionAsync(user);

            var wolf = FindAnimal(collection, "Wolf");
            Assert.Equal(1, collection.FoundCount);
            Assert.Equal(2, wolf.Count);
            Assert.Null(wolf.ImagePath);
        }

        [Fact]
        public async Task GetCollection_LeavesOutOtherUsersRecognitions()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);
            await AddImageAsync((await RegisterAsync()).UserId, "Dog", predictionScore: 0.9f);

            var collection = await GetCollectionAsync(user);

            var dog = FindAnimal(collection, "Dog");
            Assert.Equal(0, collection.FoundCount);
            Assert.Equal(0, dog.Count);
            Assert.Null(dog.ImagePath);
        }

        // A video in which no animal showed clearly is recorded as Unknown,
        // and an older model may have known animals this one does not.
        [Fact]
        public async Task GetCollection_LeavesOutAnimalsTheModelDoesNotKnow()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddRecognitionAsync(account.UserId, "Unknown", fileName: "clip.mp4");
            await AddRecognitionAsync(account.UserId, "Owl");

            var collection = await GetCollectionAsync(user);

            Assert.Equal(0, collection.FoundCount);
            Assert.Equal(Factory.Classifier.KnownAnimals, collection.Animals.Select(animal => animal.AnimalName));
        }

        [Fact]
        public async Task GetCollection_MatchesAnimalWhateverItsCase_NamingItAsTheModelDoes()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var image = await AddImageAsync(account.UserId, "coyote", predictionScore: 0.9f);

            var collection = await GetCollectionAsync(user);

            var coyote = FindAnimal(collection, "Coyote");
            Assert.Equal(1, coyote.Count);
            Assert.Equal(image.FileName, LinkedFileName(coyote.ImagePath));
        }

        // The surest image would be the cleared one, and then the video.
        [Fact]
        public async Task GetCollection_LinksTheImageTheModelWasSurestOf_AmongThoseInTheHistory()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddImageAsync(account.UserId, "Cat", predictionScore: 0.6f);
            var surest = await AddImageAsync(account.UserId, "Cat", predictionScore: 0.8f);
            await AddImageAsync(account.UserId, "Cat", predictionScore: 0.99f, isCleared: true);
            await AddRecognitionAsync(account.UserId, "Cat", fileName: "clip.mp4", predictionScore: 0.95f);

            var collection = await GetCollectionAsync(user);

            Assert.Equal(surest.FileName, LinkedFileName(FindAnimal(collection, "Cat").ImagePath));
        }

        [Fact]
        public async Task GetCollection_ImagesEquallySure_LinksTheMostRecent()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddImageAsync(account.UserId, "Fox", predictionScore: 0.7f, dateRecognized: DateTime.UtcNow.AddDays(-2));
            var latest = await AddImageAsync(account.UserId, "Fox", predictionScore: 0.7f, dateRecognized: DateTime.UtcNow);

            var collection = await GetCollectionAsync(user);

            Assert.Equal(latest.FileName, LinkedFileName(FindAnimal(collection, "Fox").ImagePath));
        }

        private static async Task<CollectionProgress> GetCollectionAsync(HttpClient client) =>
            (await client.GetFromJsonAsync<CollectionProgress>(CollectionPath))!;

        private static CollectedAnimal FindAnimal(CollectionProgress collection, string animalName) =>
            Assert.Single(collection.Animals, animal => animal.AnimalName == animalName);

        /// <summary>
        /// Adds an image's recognition along with its file, without which a
        /// link to it could not be read back.
        /// </summary>
        private async Task<AnimalRecognitionLog> AddImageAsync(string userId, string animalName, float predictionScore, bool isCleared = false, DateTime? dateRecognized = null)
        {
            var fileName = $"{Guid.NewGuid():N}.jpg";
            var directory = Path.Combine(Factory.Services.GetRequiredService<IOptions<UploadSettings>>().Value.UploadPath, userId);
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(Path.Combine(directory, fileName), []);

            return await AddRecognitionAsync(userId, animalName, fileName, dateRecognized, isCleared, predictionScore);
        }

        // Read back the way the media controller reads a link it is sent.
        private string LinkedFileName(string? imagePath)
        {
            Assert.NotNull(imagePath);
            Assert.StartsWith($"/{MediaLinkService.Route}/", imagePath);

            using var scope = Factory.Services.CreateScope();
            var token = imagePath[($"/{MediaLinkService.Route}/".Length)..];

            return Path.GetFileName(scope.ServiceProvider.GetRequiredService<IMediaLinkService>().GetPath(token));
        }
    }
}
