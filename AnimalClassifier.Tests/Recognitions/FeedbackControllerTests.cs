namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text;
    using static AnimalClassifier.Core.Recognitions.Feedback.FeedbackMessages;

    /// <summary>
    /// The animals the model knows are the stand-in classifier's, which
    /// <see cref="ApiFactory"/> puts in place of the model.
    /// </summary>
    public class FeedbackControllerTests : ApiTest
    {
        private const string FeedbackPath = "/api/feedback";

        public FeedbackControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetKnownAnimals_AreTheClassifiers()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var animals = await user.GetFromJsonAsync<List<string>>($"{FeedbackPath}/animals");

            Assert.Equal(Factory.Classifier.KnownAnimals, animals);
        }

        [Fact]
        public async Task GiveFeedback_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().PutAsJsonAsync($"{FeedbackPath}/1", Correct());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GiveFeedback_ThatTheModelWasRight_IsKeptToBeReviewed()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var details = await ReadAsync<FeedbackDetails>(await GiveFeedbackAsync(user, recognition, Correct(allowsTraining: true)));

            Assert.Equal(FeedbackVerdict.Correct, details.Verdict);
            Assert.Null(details.ActualAnimal);
            Assert.True(details.AllowsTraining);
            Assert.Equal(FeedbackReviewStatus.Pending, details.ReviewStatus);
        }

        [Fact]
        public async Task GiveFeedback_NamingAnotherAnimal_KeepsTheModelsNameForIt()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var details = await ReadAsync<FeedbackDetails>(await GiveFeedbackAsync(user, recognition, WrongAnimal("  fOX ")));

            Assert.Equal(FeedbackVerdict.WrongAnimal, details.Verdict);
            Assert.Equal("Fox", details.ActualAnimal);
        }

        [Fact]
        public async Task GiveFeedback_NamingAnAnimalTheModelDoesNotKnow_AsAKnownOne_IsRefused()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await GiveFeedbackAsync(user, recognition, WrongAnimal("Capybara"));

            await AssertRefusedAsync(response, UnknownAnimal);
        }

        [Fact]
        public async Task GiveFeedback_NamingTheAnimalTheModelNamed_AsAnotherOne_IsRefused()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await GiveFeedbackAsync(user, recognition, WrongAnimal(recognition.AnimalName.ToLower()));

            await AssertRefusedAsync(response, SameAnimal);
        }

        [Theory]
        [InlineData(FeedbackVerdict.WrongAnimal)]
        [InlineData(FeedbackVerdict.UnlistedAnimal)]
        public async Task GiveFeedback_ThatTheModelWasWrong_WithoutAnAnimal_IsRefused(FeedbackVerdict verdict)
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await GiveFeedbackAsync(user, recognition, new FeedbackRequest { Verdict = verdict, ActualAnimal = "  " });

            await AssertRefusedAsync(response, MissingAnimal);
        }

        [Fact]
        public async Task GiveFeedback_ThatTheModelWasRight_NamingAnAnimal_IsRefused()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await GiveFeedbackAsync(user, recognition, new FeedbackRequest { Verdict = FeedbackVerdict.Correct, ActualAnimal = "Dog" });

            await AssertRefusedAsync(response, AnimalNotExpected);
        }

        [Fact]
        public async Task GiveFeedback_NamingAnUnlistedAnimal_KeepsItTidiedInLowerCase()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var details = await ReadAsync<FeedbackDetails>(await GiveFeedbackAsync(user, recognition, UnlistedAnimal("  Snow   Leopard ")));

            Assert.Equal(FeedbackVerdict.UnlistedAnimal, details.Verdict);
            Assert.Equal("snow leopard", details.ActualAnimal);
        }

        [Fact]
        public async Task GiveFeedback_NamingAnAnimalTheModelKnows_AsAnUnlistedOne_IsRefused()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await GiveFeedbackAsync(user, recognition, UnlistedAnimal("wolf"));

            await AssertRefusedAsync(response, KnownAnimal);
        }

        // The name may become a folder's, in the dataset the model is trained
        // from.
        [Theory]
        [InlineData("../../secrets")]
        [InlineData("cat/dog")]
        [InlineData("=HYPERLINK(1)")]
        [InlineData("tiger2")]
        [InlineData("a-")]
        public async Task GiveFeedback_NamingAnUnlistedAnimal_WithMoreThanLetters_IsRefused(string name)
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await GiveFeedbackAsync(user, recognition, UnlistedAnimal(name));

            await AssertRefusedAsync(response, string.Format(InvalidAnimalName, RecognitionFeedback.MaxActualAnimalLength));
        }

        [Fact]
        public async Task GiveFeedback_NamingAnUnlistedAnimal_TooLong_IsRefused()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await GiveFeedbackAsync(user, recognition, UnlistedAnimal(new string('a', RecognitionFeedback.MaxActualAnimalLength + 1)));

            await AssertRefusedAsync(response, string.Format(InvalidAnimalName, RecognitionFeedback.MaxActualAnimalLength));
        }

        [Fact]
        public async Task GiveFeedback_WithABlankComment_KeepsNone()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();
            var request = Correct();
            request.Comment = "   ";

            var details = await ReadAsync<FeedbackDetails>(await GiveFeedbackAsync(user, recognition, request));

            Assert.Null(details.Comment);
        }

        [Fact]
        public async Task GiveFeedback_WithATooLongComment_IsRefused()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();
            var request = Correct();
            request.Comment = new string('a', RecognitionFeedback.MaxCommentLength + 1);

            var response = await GiveFeedbackAsync(user, recognition, request);

            await AssertRefusedAsync(response, string.Format(CommentTooLong, RecognitionFeedback.MaxCommentLength));
        }

        [Theory]
        [InlineData("""{ "verdict": "Maybe" }""")]
        [InlineData("""{ "verdict": 7 }""")]
        [InlineData("""{ "actualAnimal": "Dog" }""")]
        public async Task GiveFeedback_WithoutAVerdictItKnows_IsABadRequest(string body)
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await user.PutAsync($"{FeedbackPath}/{recognition.Id}", new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GiveFeedback_OnAVideo_IsRefused()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var recognition = await AddRecognitionAsync(account.UserId, fileName: $"{Guid.NewGuid():N}.mp4");

            var response = await GiveFeedbackAsync(user, recognition, Correct());

            await AssertRefusedAsync(response, ImagesOnly);
        }

        [Fact]
        public async Task GiveFeedback_OnAnotherUsersRecognition_ReturnsNotFound()
        {
            var (_, recognition) = await SignInWithRecognitionAsync();
            var other = await SignInAsync((await RegisterAsync()).Email);

            var response = await GiveFeedbackAsync(other, recognition, Correct());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(await FindFeedbackAsync(recognition.Id));
        }

        [Fact]
        public async Task GiveFeedback_Again_ReplacesItAndWaitsForAnotherReview()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();
            (await GiveFeedbackAsync(user, recognition, Correct(allowsTraining: true))).EnsureSuccessStatusCode();
            await SetReviewStatusAsync(recognition.Id, FeedbackReviewStatus.Accepted);

            (await GiveFeedbackAsync(user, recognition, WrongAnimal("Dog"))).EnsureSuccessStatusCode();

            var feedback = await FindFeedbackAsync(recognition.Id);
            Assert.NotNull(feedback);
            Assert.Equal(FeedbackVerdict.WrongAnimal, feedback.Verdict);
            Assert.Equal("Dog", feedback.ActualAnimal);
            Assert.False(feedback.AllowsTraining);
            Assert.Equal(FeedbackReviewStatus.Pending, feedback.ReviewStatus);
            Assert.Null(feedback.DateReviewed);
        }

        [Fact]
        public async Task WithdrawFeedback_RemovesIt()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();
            (await GiveFeedbackAsync(user, recognition, Correct())).EnsureSuccessStatusCode();

            var response = await user.DeleteAsync($"{FeedbackPath}/{recognition.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await FindFeedbackAsync(recognition.Id));
        }

        [Fact]
        public async Task WithdrawFeedback_ThatWasNeverGiven_ReturnsNotFound()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();

            var response = await user.DeleteAsync($"{FeedbackPath}/{recognition.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WithdrawFeedback_OnAnotherUsersRecognition_LeavesIt()
        {
            var (user, recognition) = await SignInWithRecognitionAsync();
            (await GiveFeedbackAsync(user, recognition, Correct())).EnsureSuccessStatusCode();
            var other = await SignInAsync((await RegisterAsync()).Email);

            var response = await other.DeleteAsync($"{FeedbackPath}/{recognition.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(await FindFeedbackAsync(recognition.Id));
        }

        private static FeedbackRequest Correct(bool allowsTraining = false) =>
            new() { Verdict = FeedbackVerdict.Correct, AllowsTraining = allowsTraining };

        private static FeedbackRequest WrongAnimal(string animal) =>
            new() { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = animal };

        private static FeedbackRequest UnlistedAnimal(string animal) =>
            new() { Verdict = FeedbackVerdict.UnlistedAnimal, ActualAnimal = animal };

        private async Task<(HttpClient User, AnimalRecognitionLog Recognition)> SignInWithRecognitionAsync()
        {
            var account = await RegisterAsync();

            return (await SignInAsync(account.Email), await AddRecognitionAsync(account.UserId));
        }

        private static Task<HttpResponseMessage> GiveFeedbackAsync(HttpClient user, AnimalRecognitionLog recognition, FeedbackRequest request) =>
            user.PutAsJsonAsync($"{FeedbackPath}/{recognition.Id}", request);

        private static async Task AssertRefusedAsync(HttpResponseMessage response, string message)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(message, (await response.Content.ReadFromJsonAsync<MessageResponse>())!.Message);
        }

        private async Task<RecognitionFeedback?> FindFeedbackAsync(int recognitionId)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            return await context.RecognitionFeedback.AsNoTracking().SingleOrDefaultAsync(f => f.RecognitionId == recognitionId);
        }

        // As an administrator's review would leave it.
        private async Task SetReviewStatusAsync(int recognitionId, FeedbackReviewStatus status)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            await context.RecognitionFeedback
                .Where(f => f.RecognitionId == recognitionId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(f => f.ReviewStatus, status)
                    .SetProperty(f => f.DateReviewed, DateTime.UtcNow));
        }
    }
}
