namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Core.Recognitions.Training.Models;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text;
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

        // So far past the last that the feedback before it numbers more than
        // an int holds.
        [Fact]
        public async Task GetFeedback_OnTheLastPagePossible_IsEmpty()
        {
            var admin = await SignInAdministratorAsync();

            var result = await admin.GetFromJsonAsync<PagedResult<FeedbackReviewItem>>($"{ReviewPath}?page={int.MaxValue}");

            Assert.Empty(result!.Items);
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

        // The other tests add to the counts as well, so this compares them
        // before and after its own feedback.
        [Fact]
        public async Task GetSummary_CountsTheVerdictsAndTheMistakes()
        {
            var admin = await SignInAdministratorAsync();
            var before = await GetSummaryAsync(admin);

            await GiveFeedbackAsync(new FeedbackRequest { Verdict = FeedbackVerdict.Correct });
            await GiveFeedbackAsync(new FeedbackRequest { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = "Dog" }, recognizedAnimal: "Coyote");
            await GiveFeedbackAsync(WrongAnimal("Dog"), recognizedAnimal: "Coyote");
            await GiveFeedbackAsync(new FeedbackRequest { Verdict = FeedbackVerdict.UnlistedAnimal, ActualAnimal = "Aardwolf" });
            await GiveFeedbackAsync(new FeedbackRequest { Verdict = FeedbackVerdict.UnlistedAnimal, ActualAnimal = "aardwolf", AllowsTraining = true });

            var after = await GetSummaryAsync(admin);

            Assert.Equal(5, after.TotalCount - before.TotalCount);
            Assert.Equal(1, after.CorrectCount - before.CorrectCount);
            Assert.Equal(2, after.WrongAnimalCount - before.WrongAnimalCount);
            Assert.Equal(2, after.UnlistedAnimalCount - before.UnlistedAnimalCount);
            Assert.Equal(2, after.PendingCount - before.PendingCount);
            Assert.Equal(2, CountOf(after.CommonMistakes, "Coyote", "Dog") - CountOf(before.CommonMistakes, "Coyote", "Dog"));
            Assert.Equal(2, Assert.Single(after.RequestedAnimals, animal => animal.Animal == "aardwolf").Count);
        }

        [Fact]
        public async Task GetSummary_AsUser_ReturnsForbidden()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{ReviewPath}/summary");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ExportTrainingData_AsUser_ReturnsForbidden()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"{ReviewPath}/export");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ExportTrainingData_HoldsEachAcceptedImage_InItsAnimalsFolder()
        {
            var admin = await SignInAdministratorAsync();
            var (corrected, correctedImage) = await GiveFeedbackOnUploadAsync(WrongAnimal("Fox"));
            var (confirmed, _) = await GiveFeedbackOnUploadAsync(new FeedbackRequest { Verdict = FeedbackVerdict.Correct, AllowsTraining = true });
            var (unlisted, _) = await GiveFeedbackOnUploadAsync(new FeedbackRequest
            {
                Verdict = FeedbackVerdict.UnlistedAnimal,
                ActualAnimal = "Capybara",
                AllowsTraining = true
            });
            foreach (var feedbackId in new[] { corrected, confirmed, unlisted })
            {
                (await AcceptAsync(admin, feedbackId)).EnsureSuccessStatusCode();
            }

            using var archive = await ExportTrainingDataAsync(admin);

            Assert.Equal(correctedImage, await ReadEntryAsync(archive, $"dataset/Fox/{corrected}.jpg"));
            Assert.NotNull(archive.GetEntry($"dataset/{Factory.Classifier.Animal}/{confirmed}.jpg"));
            Assert.NotNull(archive.GetEntry($"unlisted/capybara/{unlisted}.jpg"));
            var manifest = Encoding.UTF8.GetString(await ReadEntryAsync(archive, "manifest.csv"));
            Assert.Contains($"dataset/Fox/{corrected}.jpg,Fox,{Factory.Classifier.Animal},", manifest);
        }

        [Fact]
        public async Task ExportTrainingData_LeavesOutWhatWasNotAccepted()
        {
            var admin = await SignInAdministratorAsync();
            var (pending, _) = await GiveFeedbackOnUploadAsync(WrongAnimal("Fox"));
            var (rejected, _) = await GiveFeedbackOnUploadAsync(WrongAnimal("Fox"));
            (await admin.PostAsync($"{ReviewPath}/{rejected}/reject", null)).EnsureSuccessStatusCode();
            var (withheld, _) = await GiveFeedbackOnUploadAsync(new FeedbackRequest { Verdict = FeedbackVerdict.WrongAnimal, ActualAnimal = "Fox" });

            using var archive = await ExportTrainingDataAsync(admin);

            foreach (var feedbackId in new[] { pending, rejected, withheld })
            {
                Assert.Null(archive.GetEntry($"dataset/Fox/{feedbackId}.jpg"));
            }
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
        private async Task<int> GiveFeedbackAsync(FeedbackRequest request, string recognizedAnimal = "Cat")
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var recognition = await AddRecognitionAsync(account.UserId, recognizedAnimal);

            (await user.PutAsJsonAsync($"/api/feedback/{recognition.Id}", request)).EnsureSuccessStatusCode();

            return await FeedbackIdOfAsync(recognition.Id);
        }

        private async Task<int> FeedbackIdOfAsync(int recognitionId)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            return await context.RecognitionFeedback
                .Where(f => f.RecognitionId == recognitionId)
                .Select(f => f.Id)
                .SingleAsync();
        }

        /// <summary>
        /// Gives feedback on a new user's recognition of an image they
        /// uploaded, whose file is there to be exported.
        /// </summary>
        /// <returns>The feedback's id, and the image's contents.</returns>
        private async Task<(int FeedbackId, byte[] Image)> GiveFeedbackOnUploadAsync(FeedbackRequest request)
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var fileName = $"{Guid.NewGuid():N}.jpg";
            var image = Guid.NewGuid().ToByteArray();

            var uploadPath = Factory.Services.GetRequiredService<IOptions<UploadSettings>>().Value.UploadPath;
            Directory.CreateDirectory(Path.Combine(uploadPath, account.UserId));
            await File.WriteAllBytesAsync(Path.Combine(uploadPath, account.UserId, fileName), image);

            var recognition = await AddRecognitionAsync(account.UserId, fileName: fileName);
            (await user.PutAsJsonAsync($"/api/feedback/{recognition.Id}", request)).EnsureSuccessStatusCode();

            return (await FeedbackIdOfAsync(recognition.Id), image);
        }

        private static async Task<ZipArchive> ExportTrainingDataAsync(HttpClient admin)
        {
            var response = await admin.GetAsync($"{ReviewPath}/export");
            response.EnsureSuccessStatusCode();

            return new ZipArchive(await response.Content.ReadAsStreamAsync());
        }

        private static async Task<byte[]> ReadEntryAsync(ZipArchive archive, string entryName)
        {
            var entry = archive.GetEntry(entryName);
            Assert.NotNull(entry);

            await using var contents = await entry.OpenAsync();
            using var read = new MemoryStream();
            await contents.CopyToAsync(read);

            return read.ToArray();
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

        private static async Task<FeedbackSummary> GetSummaryAsync(HttpClient admin) =>
            (await admin.GetFromJsonAsync<FeedbackSummary>($"{ReviewPath}/summary"))!;

        private static int CountOf(IEnumerable<CommonMistake> mistakes, string recognizedAnimal, string actualAnimal) =>
            mistakes.SingleOrDefault(mistake => mistake.RecognizedAnimal == recognizedAnimal && mistake.ActualAnimal == actualAnimal)?.Count ?? 0;

        private static void AssertItem(IEnumerable<FeedbackReviewItem> items, int feedbackId, string label, bool isKnownAnimal)
        {
            var item = Assert.Single(items, item => item.Id == feedbackId);

            Assert.Equal(label, item.Label);
            Assert.Equal(isKnownAnimal, item.IsKnownAnimal);
        }
    }
}
