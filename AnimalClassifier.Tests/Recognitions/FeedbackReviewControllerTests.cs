namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Core.Recognitions.Training.Models;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Recognitions.Training.TrainingMessages;

    /// <summary>
    /// Every test in the class shares one database, and with it one review,
    /// so each looks only for the feedback it gave.
    /// </summary>
    public class FeedbackReviewControllerTests : ApiTest
    {
        private const string ReviewPath = "/api/admin/feedback";

        public FeedbackReviewControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetFeedback_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().GetAsync(ReviewPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedback_AsUser_ReturnsForbidden()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync(ReviewPath);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedback_ListsWhatWaitsForReview_WithWhatItWouldBeTrainedAs()
        {
            var admin = await SignInAdministratorAsync();
            var corrected = await GiveFeedbackAsync(WrongAnimal("Fox"));
            var confirmed = await GiveFeedbackAsync(new FeedbackRequest { Verdict = FeedbackVerdict.Correct, AllowsTraining = true });
            var unlisted = await GiveFeedbackAsync(new FeedbackRequest
            {
                Verdict = FeedbackVerdict.UnlistedAnimal,
                ActualAnimal = "Capybara",
                AllowsTraining = true
            });

            var pending = await GetReviewAsync(admin, FeedbackReviewStatus.Pending);

            AssertItem(pending, corrected, label: "Fox", isKnownAnimal: true);
            AssertItem(pending, confirmed, label: Factory.Classifier.Animal, isKnownAnimal: true);
            AssertItem(pending, unlisted, label: "capybara", isKnownAnimal: false);
        }

        [Fact]
        public async Task GetFeedback_LeavesOutWhatDoesNotAllowTraining()
        {
            var admin = await SignInAdministratorAsync();
            var feedbackId = await GiveFeedbackAsync(new FeedbackRequest { Verdict = FeedbackVerdict.Correct });

            var pending = await GetReviewAsync(admin, FeedbackReviewStatus.Pending);

            Assert.DoesNotContain(pending, item => item.Id == feedbackId);
        }

        [Theory]
        [InlineData("Maybe")]
        [InlineData("7")]
        public async Task GetFeedback_WithAStatusItDoesNotKnow_IsABadRequest(string status)
        {
            var admin = await SignInAdministratorAsync();

            var response = await admin.GetAsync($"{ReviewPath}?status={status}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Accept_MovesItFromPendingToAccepted()
        {
            var admin = await SignInAdministratorAsync();
            var feedbackId = await GiveFeedbackAsync(WrongAnimal("Wolf"));

            var response = await AcceptAsync(admin, feedbackId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.DoesNotContain(await GetReviewAsync(admin, FeedbackReviewStatus.Pending), item => item.Id == feedbackId);
            var accepted = Assert.Single(await GetReviewAsync(admin, FeedbackReviewStatus.Accepted), item => item.Id == feedbackId);
            Assert.NotNull(accepted.DateReviewed);
        }

        [Fact]
        public async Task Accept_WhatIsAcceptedAlready_IsRefused()
        {
            var admin = await SignInAdministratorAsync();
            var feedbackId = await GiveFeedbackAsync(WrongAnimal("Fox"));
            (await AcceptAsync(admin, feedbackId)).EnsureSuccessStatusCode();

            var response = await AcceptAsync(admin, feedbackId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(FeedbackAlreadyAccepted, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reject_WhatWasAccepted_TakesItBack()
        {
            var admin = await SignInAdministratorAsync();
            var feedbackId = await GiveFeedbackAsync(WrongAnimal("Dog"));
            (await AcceptAsync(admin, feedbackId)).EnsureSuccessStatusCode();

            var response = await admin.PostAsync($"{ReviewPath}/{feedbackId}/reject", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.DoesNotContain(await GetReviewAsync(admin, FeedbackReviewStatus.Accepted), item => item.Id == feedbackId);
            Assert.Contains(await GetReviewAsync(admin, FeedbackReviewStatus.Rejected), item => item.Id == feedbackId);
        }

        [Fact]
        public async Task Reject_FeedbackThatDoesNotExist_ReturnsNotFound()
        {
            var admin = await SignInAdministratorAsync();

            var response = await admin.PostAsync($"{ReviewPath}/{int.MaxValue}/reject", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Its user kept it out of training, so it is not anyone's to review.
        [Fact]
        public async Task Accept_FeedbackThatDoesNotAllowTraining_ReturnsNotFound()
        {
            var admin = await SignInAdministratorAsync();
            var feedbackId = await GiveFeedbackAsync(new FeedbackRequest { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = "Fox" });

            var response = await AcceptAsync(admin, feedbackId);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Accept_AsUser_ReturnsForbidden()
        {
            var feedbackId = await GiveFeedbackAsync(WrongAnimal("Fox"));
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await AcceptAsync(user, feedbackId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private static FeedbackRequest WrongAnimal(string animal) =>
            new() { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = animal, AllowsTraining = true };

        private async Task<HttpClient> SignInAdministratorAsync()
        {
            var account = await RegisterAsync();
            await MakeAdministratorAsync(account.UserId);

            return await SignInAsync(account.Email);
        }

        /// <summary>
        /// Gives feedback on a new user's new recognition, as they would.
        /// </summary>
        /// <returns>
        /// The feedback's id, which the review names it by, and which only the
        /// database tells, as nothing a user is answered with does.
        /// </returns>
        private async Task<int> GiveFeedbackAsync(FeedbackRequest request)
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var recognition = await AddRecognitionAsync(account.UserId);

            (await user.PutAsJsonAsync($"/api/feedback/{recognition.Id}", request)).EnsureSuccessStatusCode();

            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            return await context.RecognitionFeedback
                .Where(f => f.RecognitionId == recognition.Id)
                .Select(f => f.Id)
                .SingleAsync();
        }

        private static Task<HttpResponseMessage> AcceptAsync(HttpClient client, int feedbackId) =>
            client.PostAsync($"{ReviewPath}/{feedbackId}/accept", null);

        // Every page of it, since the other tests add to it.
        private static async Task<List<FeedbackReviewItem>> GetReviewAsync(HttpClient admin, FeedbackReviewStatus status)
        {
            var items = new List<FeedbackReviewItem>();

            for (var page = 1; ; page++)
            {
                var result = await admin.GetFromJsonAsync<PagedResult<FeedbackReviewItem>>($"{ReviewPath}?status={status}&page={page}");
                items.AddRange(result!.Items);

                if (result.Items.Count == 0 || items.Count >= result.TotalCount)
                {
                    return items;
                }
            }
        }

        private static void AssertItem(IEnumerable<FeedbackReviewItem> items, int feedbackId, string label, bool isKnownAnimal)
        {
            var item = Assert.Single(items, item => item.Id == feedbackId);

            Assert.Equal(label, item.Label);
            Assert.Equal(isKnownAnimal, item.IsKnownAnimal);
        }
    }
}
