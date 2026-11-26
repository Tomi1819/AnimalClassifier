namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Recognitions.Achievements;
    using AnimalClassifier.Core.Recognitions.Achievements.Models;
    using AnimalClassifier.Core.Recognitions.Collection;
    using AnimalClassifier.Core.Recognitions.Collection.Models;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Tests.Support;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Http.Json;

    /// <summary>
    /// The achievements as the database gives them: what each recognition
    /// finds, and the feedback on it. The rules themselves are tested in
    /// <see cref="AchievementEvaluatorTests"/>.
    /// </summary>
    public class AchievementsControllerTests : ApiTest
    {
        private const string AchievementsPath = "/api/achievements";
        private const string CollectionPath = "/api/collection";
        private const string FeedbackPath = "/api/feedback";
        private const string TokyoTimeZone = "Asia/Tokyo";
        private const string UnknownTimeZone = "Nowhere/Nothing";

        // Sure enough to find an animal however the minimum is set.
        private const float SureScore = 1f;

        public AchievementsControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        private float MinimumScore => Factory.Services.GetRequiredService<IOptions<CollectionSettings>>().Value.MinimumScore;

        // Just short of it, as far as a float tells the two apart.
        private float UnsureScore => MathF.BitDecrement(MinimumScore);

        [Fact]
        public async Task GetAchievements_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().GetAsync(AchievementsPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAchievements_AtFirst_IsTheFirstLevel_WithEveryBadgeToEarn()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var achievements = await GetAchievementsAsync(user);

            Assert.Equal(0, achievements.FoundCount);
            Assert.Equal(Factory.Classifier.KnownAnimals.Count, achievements.KnownCount);
            Assert.Equal(Levels.FirstLevelName, achievements.Level.Name);
            Assert.Equal(
                Levels.For(Factory.Classifier.KnownAnimals.Count).Select(level => level.Name),
                achievements.Levels.Select(level => level.Name));
            Assert.Equal(Badges.All.Select(badge => badge.Id), achievements.Badges.Select(badge => badge.Id));
            Assert.DoesNotContain(achievements.Badges, badge => badge.IsEarned);
        }

        // A cleared image, a video, one the user said was right however unsure
        // the model was, and one they corrected all find their animal; one the
        // model was not sure enough of finds none.
        [Fact]
        public async Task GetAchievements_CountsTheAnimalsFound_AsTheCollectionDoes()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddRecognitionAsync(account.UserId, "Fox", isCleared: true, predictionScore: SureScore);
            await AddRecognitionAsync(account.UserId, "Coyote", fileName: "clip.mp4", predictionScore: SureScore);
            await AddRecognitionAsync(account.UserId, "Dog", predictionScore: UnsureScore);
            var confirmed = await AddRecognitionAsync(account.UserId, "Cat", predictionScore: UnsureScore);
            var corrected = await AddRecognitionAsync(account.UserId, "Cat", predictionScore: SureScore);
            await GiveFeedbackAsync(user, confirmed, new FeedbackRequest { Verdict = FeedbackVerdict.Correct });
            await GiveFeedbackAsync(user, corrected, new FeedbackRequest { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = "Wolf" });

            var achievements = await GetAchievementsAsync(user);
            var collection = await ReadAsync<CollectionProgress>(await user.GetAsync(CollectionPath));

            Assert.Equal(4, achievements.FoundCount);
            Assert.Equal(collection.FoundCount, achievements.FoundCount);
            Assert.Equal("Spotter", achievements.Level.Name);
            Assert.True(Badge(achievements, Badges.FilmMaker).IsEarned);
            Assert.Equal(1, Badge(achievements, Badges.Corrector).Progress);
            Assert.Equal(2, Badge(achievements, Badges.Teacher).Progress);
        }

        [Fact]
        public async Task GetAchievements_LeavesOutOtherUsersRecognitions()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);
            await AddRecognitionAsync((await RegisterAsync()).UserId, "Dog", predictionScore: SureScore);

            var achievements = await GetAchievementsAsync(user);

            Assert.Equal(0, achievements.FoundCount);
            Assert.False(Badge(achievements, Badges.FirstFind).IsEarned);
        }

        [Fact]
        public async Task GetAchievements_ImageAcceptedForTraining_EarnsHelpingHand()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var shared = await AddRecognitionAsync(account.UserId, "Cat", predictionScore: SureScore);
            await GiveFeedbackAsync(user, shared, new FeedbackRequest { Verdict = FeedbackVerdict.Correct, AllowsTraining = true });

            var beforeReview = Badge(await GetAchievementsAsync(user), Badges.HelpingHand);
            await ReviewFeedbackAsync(shared, FeedbackReviewStatus.Accepted);
            var afterReview = Badge(await GetAchievementsAsync(user), Badges.HelpingHand);

            Assert.False(beforeReview.IsEarned);
            Assert.True(afterReview.IsEarned);
        }

        [Fact]
        public async Task GetAchievements_ImageRejectedForTraining_EarnsNoHelpingHand()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var shared = await AddRecognitionAsync(account.UserId, "Cat", predictionScore: SureScore);
            await GiveFeedbackAsync(user, shared, new FeedbackRequest { Verdict = FeedbackVerdict.Correct, AllowsTraining = true });

            await ReviewFeedbackAsync(shared, FeedbackReviewStatus.Rejected);

            Assert.False(Badge(await GetAchievementsAsync(user), Badges.HelpingHand).IsEarned);
        }

        // 14:00 and 16:00 in UTC are on one day there, and either side of
        // midnight in Tokyo.
        [Fact]
        public async Task GetAchievements_InTimeZone_CountsTheDaysInARowThere()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var day = DateTime.UtcNow.Date.AddDays(-3);
            await AddRecognitionAsync(account.UserId, dateRecognized: day.AddHours(14));
            await AddRecognitionAsync(account.UserId, dateRecognized: day.AddHours(16));

            var utc = Badge(await GetAchievementsAsync(user), Badges.OnARoll);
            var tokyo = Badge(await GetAchievementsAsync(user, TokyoTimeZone), Badges.OnARoll);

            Assert.Equal(1, utc.Progress);
            Assert.Equal(2, tokyo.Progress);
        }

        [Fact]
        public async Task GetAchievements_UnknownTimeZone_ReturnsBadRequest()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{AchievementsPath}?timeZone={UnknownTimeZone}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetRecognitionAchievements_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().GetAsync($"{AchievementsPath}/recognitions/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetRecognitionAchievements_OfAnotherUsersRecognition_ReturnsNotFound()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);
            var othersRecognition = await AddRecognitionAsync((await RegisterAsync()).UserId, predictionScore: SureScore);

            var response = await user.GetAsync(RecognitionPath(othersRecognition));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetRecognitionAchievements_OfNoRecognition_ReturnsNotFound()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{AchievementsPath}/recognitions/{int.MaxValue}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetRecognitionAchievements_UnknownTimeZone_ReturnsBadRequest()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var recognition = await AddRecognitionAsync(account.UserId, predictionScore: SureScore);

            var response = await user.GetAsync($"{RecognitionPath(recognition)}?timeZone={UnknownTimeZone}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetRecognitionAchievements_OfTheFirstFind_EarnsItsBadges_ButNoLevel()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var first = await AddRecognitionAsync(account.UserId, "Fox", predictionScore: SureScore);

            var earned = await GetRecognitionAchievementsAsync(user, first);

            Assert.Null(earned.NewLevel);
            Assert.Equal([Badges.FirstFind, Badges.SharpEye], earned.NewBadges.Select(badge => badge.Id));
        }

        [Fact]
        public async Task GetRecognitionAchievements_OfTheFindThatReachesALevel_EarnsTheLevel()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddRecognitionAsync(account.UserId, "Cat", dateRecognized: DateTime.UtcNow.AddDays(-2), predictionScore: 0.9f);
            await AddRecognitionAsync(account.UserId, "Dog", dateRecognized: DateTime.UtcNow.AddDays(-1), predictionScore: 0.9f);
            var third = await AddRecognitionAsync(account.UserId, "Fox", predictionScore: 0.9f);

            var earned = await GetRecognitionAchievementsAsync(user, third);

            Assert.Equal("Spotter", earned.NewLevel?.Name);
            Assert.Empty(earned.NewBadges);
        }

        [Fact]
        public async Task GetRecognitionAchievements_OfAnAnimalFoundBefore_EarnsNothing()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            await AddRecognitionAsync(account.UserId, "Cat", dateRecognized: DateTime.UtcNow.AddDays(-1), predictionScore: SureScore);
            var again = await AddRecognitionAsync(account.UserId, "Cat", predictionScore: SureScore);

            var earned = await GetRecognitionAchievementsAsync(user, again);

            Assert.Null(earned.NewLevel);
            Assert.Empty(earned.NewBadges);
        }

        // As the collection waits for it, so do the badges of finding.
        [Fact]
        public async Task GetRecognitionAchievements_OfOneTheModelWasNotSureEnoughOf_EarnsTheFindOnceConfirmed()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var unsure = await AddRecognitionAsync(account.UserId, "Wolf", predictionScore: UnsureScore);

            var beforeFeedback = await GetRecognitionAchievementsAsync(user, unsure);
            await GiveFeedbackAsync(user, unsure, new FeedbackRequest { Verdict = FeedbackVerdict.Correct });
            var afterFeedback = await GetRecognitionAchievementsAsync(user, unsure);

            Assert.DoesNotContain(beforeFeedback.NewBadges, badge => badge.Id == Badges.FirstFind);
            Assert.Contains(afterFeedback.NewBadges, badge => badge.Id == Badges.FirstFind);
        }

        private static string RecognitionPath(AnimalRecognitionLog recognition) =>
            $"{AchievementsPath}/recognitions/{recognition.Id}";

        private static async Task<AchievementProgress> GetAchievementsAsync(HttpClient client, string? timeZone = null) =>
            await ReadAsync<AchievementProgress>(await client.GetAsync(
                timeZone is null ? AchievementsPath : $"{AchievementsPath}?timeZone={Uri.EscapeDataString(timeZone)}"));

        private static async Task<RecognitionAchievements> GetRecognitionAchievementsAsync(HttpClient client, AnimalRecognitionLog recognition) =>
            await ReadAsync<RecognitionAchievements>(await client.GetAsync(RecognitionPath(recognition)));

        private static BadgeProgress Badge(AchievementProgress achievements, string id) =>
            achievements.Badges.Single(badge => badge.Id == id);

        private static async Task GiveFeedbackAsync(HttpClient user, AnimalRecognitionLog recognition, FeedbackRequest request) =>
            (await user.PutAsJsonAsync($"{FeedbackPath}/{recognition.Id}", request)).EnsureSuccessStatusCode();

        // Straight in the database, as an administrator's review would leave
        // it; the review itself is tested in FeedbackReviewControllerTests.
        private async Task ReviewFeedbackAsync(AnimalRecognitionLog recognition, FeedbackReviewStatus status)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            var feedback = await context.RecognitionFeedback.SingleAsync(f => f.RecognitionId == recognition.Id);
            feedback.ReviewStatus = status;
            feedback.DateReviewed = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
    }
}
