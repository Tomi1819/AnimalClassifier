namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Recognitions.Collection;
    using AnimalClassifier.Core.Recognitions.Collection.Models;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
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
        private const string FeedbackPath = "/api/feedback";

        // Sure enough to find an animal however the minimum is set.
        private const float SureScore = 1f;

        public CollectionControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        private float MinimumScore => Factory.Services.GetRequiredService<IOptions<CollectionSettings>>().Value.MinimumScore;

        // Just short of it, as far as a float tells the two apart.
        private float UnsureScore => MathF.BitDecrement(MinimumScore);

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
            await AddRecognitionAsync(account.UserId, "Fox", dateRecognized: DateTime.UtcNow.AddDays(-1), predictionScore: SureScore);
            await AddRecognitionAsync(account.UserId, "Fox", dateRecognized: firstFound, predictionScore: SureScore);

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
            await AddRecognitionAsync(account.UserId, "Unknown", fileName: "clip.mp4", predictionScore: SureScore);
            await AddRecognitionAsync(account.UserId, "Owl", predictionScore: SureScore);

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

        [Fact]
        public async Task GetCollection_RecognitionTheModelWasNotSureEnoughOf_FindsNothing()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddImageAsync(account.UserId, "Fox", predictionScore: UnsureScore);

            var collection = await GetCollectionAsync(user);

            var fox = FindAnimal(collection, "Fox");
            Assert.Equal(0, collection.FoundCount);
            Assert.Equal(0, fox.Count);
            Assert.Null(fox.ImagePath);
        }

        [Fact]
        public async Task GetCollection_RecognitionAtTheMinimumScore_FindsItsAnimal()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddImageAsync(account.UserId, "Fox", predictionScore: MinimumScore);

            var collection = await GetCollectionAsync(user);

            Assert.Equal(1, FindAnimal(collection, "Fox").Count);
        }

        [Fact]
        public async Task GetCollection_RecognitionTheUserSaysWasRight_FindsItsAnimal_HoweverUnsureTheModelWas()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var image = await AddImageAsync(account.UserId, "Fox", predictionScore: UnsureScore);
            await GiveFeedbackAsync(user, image, new FeedbackRequest { Verdict = FeedbackVerdict.Correct });

            var collection = await GetCollectionAsync(user);

            var fox = FindAnimal(collection, "Fox");
            Assert.Equal(1, collection.FoundCount);
            Assert.Equal(1, fox.Count);
            Assert.Equal(image.FileName, LinkedFileName(fox.ImagePath));
        }

        [Fact]
        public async Task GetCollection_RecognitionTheUserCorrected_FindsTheAnimalTheyNamed_InPlaceOfTheModels()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var image = await AddImageAsync(account.UserId, "Wolf", predictionScore: SureScore);
            await GiveFeedbackAsync(user, image, new FeedbackRequest { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = "Dog" });

            var collection = await GetCollectionAsync(user);

            var dog = FindAnimal(collection, "Dog");
            Assert.Equal(1, collection.FoundCount);
            Assert.Equal(1, dog.Count);
            Assert.Equal(image.FileName, LinkedFileName(dog.ImagePath));
            Assert.Equal(0, FindAnimal(collection, "Wolf").Count);
        }

        [Fact]
        public async Task GetCollection_RecognitionOfAnAnimalTheModelDoesNotKnow_FindsNothing()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var image = await AddImageAsync(account.UserId, "Cat", predictionScore: SureScore);
            await GiveFeedbackAsync(user, image, new FeedbackRequest { Verdict = FeedbackVerdict.UnlistedAnimal, ActualAnimal = "capybara" });

            var collection = await GetCollectionAsync(user);

            Assert.Equal(0, collection.FoundCount);
            Assert.Equal(0, FindAnimal(collection, "Cat").Count);
        }

        // The user knows what the image shows, where the model only guessed.
        [Fact]
        public async Task GetCollection_LinksAnImageTheUserSaysShowsTheAnimal_BeforeOneOnlyTheModelWasSureOf()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddImageAsync(account.UserId, "Cat", predictionScore: SureScore);
            var confirmed = await AddImageAsync(account.UserId, "Cat", predictionScore: MinimumScore);
            await GiveFeedbackAsync(user, confirmed, new FeedbackRequest { Verdict = FeedbackVerdict.Correct });

            var collection = await GetCollectionAsync(user);

            Assert.Equal(confirmed.FileName, LinkedFileName(FindAnimal(collection, "Cat").ImagePath));
        }

        [Fact]
        public async Task GetFind_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().GetAsync($"{CollectionPath}/recognitions/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFind_OfAnotherUsersRecognition_ReturnsNotFound()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);
            var theirs = await AddRecognitionAsync((await RegisterAsync()).UserId, "Fox", predictionScore: SureScore);

            var response = await user.GetAsync(FindPath(theirs));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetFind_OfTheFirstRecognitionOfAnAnimal_IsItsFirstFind()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddRecognitionAsync(account.UserId, "Cat", predictionScore: SureScore);
            var fox = await AddRecognitionAsync(account.UserId, "fox", predictionScore: SureScore);

            var find = await GetFindAsync(user, fox);

            Assert.Equal(FindStatus.FirstFind, find.Status);
            Assert.Equal("Fox", find.AnimalName);
            Assert.Equal(2, find.FoundCount);
            Assert.Equal(Factory.Classifier.KnownAnimals.Count, find.KnownCount);
        }

        [Fact]
        public async Task GetFind_OfALaterRecognitionOfAnAnimal_IsAlreadyFound()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddRecognitionAsync(account.UserId, "Fox", dateRecognized: DateTime.UtcNow.AddDays(-1), predictionScore: SureScore);
            var later = await AddRecognitionAsync(account.UserId, "Fox", predictionScore: SureScore);

            var find = await GetFindAsync(user, later);

            Assert.Equal(FindStatus.AlreadyFound, find.Status);
            Assert.Equal(1, find.FoundCount);
        }

        // An earlier recognition the model was not sure enough of found
        // nothing, so the first that did is the first find.
        [Fact]
        public async Task GetFind_AfterOneTheModelWasNotSureEnoughOf_IsTheFirstFind()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var unsure = await AddRecognitionAsync(account.UserId, "Fox", dateRecognized: DateTime.UtcNow.AddDays(-1), predictionScore: UnsureScore);
            var sure = await AddRecognitionAsync(account.UserId, "Fox", predictionScore: SureScore);

            Assert.Equal(FindStatus.FirstFind, (await GetFindAsync(user, sure)).Status);
            Assert.Equal(FindStatus.AlreadyFound, (await GetFindAsync(user, unsure)).Status);
        }

        [Fact]
        public async Task GetFind_OfOneTheModelWasNotSureEnoughOf_WaitsToBeConfirmed()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var unsure = await AddRecognitionAsync(account.UserId, "Fox", predictionScore: UnsureScore);

            var find = await GetFindAsync(user, unsure);

            Assert.Equal(FindStatus.Unconfirmed, find.Status);
            Assert.Equal("Fox", find.AnimalName);
            Assert.Equal(0, find.FoundCount);
        }

        [Fact]
        public async Task GetFind_OfOneTheModelWasNotSureEnoughOf_ConfirmedByTheUser_IsTheFirstFind()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var unsure = await AddRecognitionAsync(account.UserId, "Fox", predictionScore: UnsureScore);
            await GiveFeedbackAsync(user, unsure, new FeedbackRequest { Verdict = FeedbackVerdict.Correct });

            var find = await GetFindAsync(user, unsure);

            Assert.Equal(FindStatus.FirstFind, find.Status);
            Assert.Equal(1, find.FoundCount);
        }

        [Fact]
        public async Task GetFind_OfOneTheUserCorrected_FindsTheAnimalTheyNamed()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var wolf = await AddRecognitionAsync(account.UserId, "Wolf", predictionScore: SureScore);
            await GiveFeedbackAsync(user, wolf, new FeedbackRequest { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = "Dog" });

            var find = await GetFindAsync(user, wolf);

            Assert.Equal(FindStatus.FirstFind, find.Status);
            Assert.Equal("Dog", find.AnimalName);
        }

        // Neither waits to be confirmed, as nothing the user could say would
        // find an animal the model knows.
        [Fact]
        public async Task GetFind_OfOneShowingAnAnimalTheModelDoesNotKnow_FindsNone()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var unlisted = await AddRecognitionAsync(account.UserId, "Cat", predictionScore: UnsureScore);
            await GiveFeedbackAsync(user, unlisted, new FeedbackRequest { Verdict = FeedbackVerdict.UnlistedAnimal, ActualAnimal = "capybara" });
            var unknown = await AddRecognitionAsync(account.UserId, "Unknown", fileName: "clip.mp4");

            Assert.All([await GetFindAsync(user, unlisted), await GetFindAsync(user, unknown)], find =>
            {
                Assert.Equal(FindStatus.None, find.Status);
                Assert.Null(find.AnimalName);
            });
        }

        private static async Task<CollectionProgress> GetCollectionAsync(HttpClient client) =>
            (await client.GetFromJsonAsync<CollectionProgress>(CollectionPath))!;

        private static string FindPath(AnimalRecognitionLog recognition) =>
            $"{CollectionPath}/recognitions/{recognition.Id}";

        private static async Task<CollectionFind> GetFindAsync(HttpClient client, AnimalRecognitionLog recognition) =>
            (await client.GetFromJsonAsync<CollectionFind>(FindPath(recognition)))!;

        private static CollectedAnimal FindAnimal(CollectionProgress collection, string animalName) =>
            Assert.Single(collection.Animals, animal => animal.AnimalName == animalName);

        // Through the endpoint the dashboard confirms an animal by.
        private static async Task GiveFeedbackAsync(HttpClient user, AnimalRecognitionLog recognition, FeedbackRequest request) =>
            (await user.PutAsJsonAsync($"{FeedbackPath}/{recognition.Id}", request)).EnsureSuccessStatusCode();

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
