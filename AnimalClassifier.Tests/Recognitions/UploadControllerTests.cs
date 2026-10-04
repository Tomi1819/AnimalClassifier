namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.History.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Core.Recognitions.Uploads;
    using AnimalClassifier.Core.Recognitions.Uploads.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;

    /// <summary>
    /// Uploads are recognised by the stand-ins <see cref="ApiFactory"/> puts
    /// in place of the model, which see whatever a test tells them to. Tests
    /// in one class run one at a time, so a test can set them without
    /// another seeing it.
    /// </summary>
    public class UploadControllerTests : ApiTest
    {
        private const string ImagePath = "/api/upload/image";
        private const string VideoPath = "/api/upload/video";
        private const string HistoryPath = "/api/upload/history";
        private const string JpegContentType = "image/jpeg";
        private const string Mp4ContentType = "video/mp4";

        // What a JPEG file starts with, which is all that is checked of one
        // before the model reads it.
        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        public UploadControllerTests(ApiFactory factory)
            : base(factory)
        {
            Factory.Classifier.Fails = false;
            Factory.FrameSampler.Unreadable = false;
            Factory.FrameSampler.Frames = 5;
        }

        [Fact]
        public async Task UploadImage_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().PostAsync(ImagePath, ImageForm(Jpeg));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("cat.jpg", JpegContentType)]
        [InlineData("cat.png", "image/png")]
        public async Task UploadImage_IsRecognisedAndPointsToItself(string fileName, string contentType)
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            var content = fileName.EndsWith(".png") ? Png : Jpeg;

            var response = await user.PostAsync(ImagePath, ImageForm(content, fileName, contentType));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<ImageUploadResult>())!;
            Assert.Equal(Factory.Classifier.Animal, result.RecognizedAnimal);
            Assert.StartsWith($"/{MediaLinkService.Route}/", result.ImagePath);
            Assert.DoesNotContain(account.UserId, result.ImagePath);

            var readBack = await user.GetFromJsonAsync<ImageUploadResult>(response.Headers.Location);
            Assert.Equal(result.ImageId, readBack!.ImageId);
        }

        [Fact]
        public async Task UploadImage_IsAddedToTheHistory()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var result = await ReadAsync<ImageUploadResult>(await user.PostAsync(ImagePath, ImageForm(Jpeg)));

            var item = Assert.Single((await user.GetFromJsonAsync<List<RecognitionHistoryItem>>(HistoryPath))!);
            Assert.Equal(result.ImageId, item.Id);
            Assert.False(item.IsVideo);
        }

        // The name and the content type are whatever the caller says, so the
        // content is what shows it is not an image.
        [Fact]
        public async Task UploadImage_ThatIsNoImage_IsRefused()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.PostAsync(ImagePath, ImageForm("<html></html>"u8.ToArray()));

            await AssertRefusedAsync(response, UploadMessages.UnsupportedImage);
        }

        [Fact]
        public async Task UploadImage_WithAVideosExtension_IsRefused()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.PostAsync(ImagePath, ImageForm(Jpeg, "cat.mp4"));

            await AssertRefusedAsync(response, UploadMessages.UnsupportedImage);
        }

        [Fact]
        public async Task UploadImage_TooLarge_IsRefused()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);
            var tooLarge = new byte[UploadValidator.MaxFileSize + 1];
            Jpeg.CopyTo(tooLarge, 0);

            var response = await user.PostAsync(ImagePath, ImageForm(tooLarge));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // Nothing is stored for an image the model fails on, and the caller is
        // not told what failed inside the app.
        [Fact]
        public async Task UploadImage_TheModelFailsOn_LeavesNothingBehind()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            Factory.Classifier.Fails = true;

            var response = await user.PostAsync(ImagePath, ImageForm(Jpeg));

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Empty(UploadedFiles(account.UserId));
            Assert.Empty((await user.GetFromJsonAsync<List<RecognitionHistoryItem>>(HistoryPath))!);
        }

        [Fact]
        public async Task UploadVideo_IsSummedUpFromItsFrames()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);
            Factory.FrameSampler.Frames = 4;

            var result = await ReadAsync<VideoUploadResult>(await user.PostAsync(VideoPath, VideoForm()));

            Assert.Equal(4, result.FramesProcessed);
            var animal = Assert.Single(result.TopAnimals);
            Assert.Equal(Factory.Classifier.Animal, animal.Animal);
            Assert.Equal("0.90", animal.AverageScore);

            var item = Assert.Single((await user.GetFromJsonAsync<List<RecognitionHistoryItem>>(HistoryPath))!);
            Assert.True(item.IsVideo);
            Assert.Equal(4, item.FramesProcessed);
        }

        // The video is stored before its frames can be read, so one that
        // turns out unreadable has to be removed again.
        [Fact]
        public async Task UploadVideo_ThatCannotBeRead_IsRefusedAndRemoved()
        {
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);
            Factory.FrameSampler.Unreadable = true;

            var response = await user.PostAsync(VideoPath, VideoForm());

            await AssertRefusedAsync(response, ClassificationMessages.UnreadableVideo);
            Assert.Empty(UploadedFiles(account.UserId));
        }

        [Fact]
        public async Task UploadVideo_WithoutAClearAnimal_IsRecordedAsUnknown()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);
            Factory.FrameSampler.Frames = VideoSummary.MinFrames - 1;

            var result = await ReadAsync<VideoUploadResult>(await user.PostAsync(VideoPath, VideoForm()));

            Assert.Empty(result.TopAnimals);
            var item = Assert.Single((await user.GetFromJsonAsync<List<RecognitionHistoryItem>>(HistoryPath))!);
            Assert.Equal(UploadService.UnrecognisedAnimal, item.RecognizedAnimal);
        }

        // Told apart from one that does not exist by nothing, so that ids
        // cannot be tested for by asking.
        [Fact]
        public async Task GetImageUpload_AnotherUsers_ReturnsNotFound()
        {
            var other = await RegisterAsync();
            var recognition = await AddRecognitionAsync(other.UserId);
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync($"/api/upload/{recognition.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetHistory_ListsOnlyTheUsersOwn_MostRecentFirst()
        {
            var account = await RegisterAsync();
            var older = await AddRecognitionAsync(account.UserId, dateRecognized: DateTime.UtcNow.AddDays(-1));
            var newer = await AddRecognitionAsync(account.UserId, fileName: "clip.mp4");
            await AddRecognitionAsync((await RegisterAsync()).UserId);
            var user = await SignInAsync(account.Email);

            var history = (await user.GetFromJsonAsync<List<RecognitionHistoryItem>>(HistoryPath))!;

            Assert.Equal([newer.Id, older.Id], history.Select(item => item.Id));
            Assert.True(history[0].IsVideo);
        }

        [Fact]
        public async Task ClearHistory_HidesEveryRecognition()
        {
            var account = await RegisterAsync();
            await AddRecognitionAsync(account.UserId);
            await AddRecognitionAsync(account.UserId);
            var user = await SignInAsync(account.Email);

            var response = await user.DeleteAsync(HistoryPath);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty((await user.GetFromJsonAsync<List<RecognitionHistoryItem>>(HistoryPath))!);
        }

        private static MultipartFormDataContent ImageForm(byte[] content, string fileName = "cat.jpg", string contentType = JpegContentType) =>
            Form("formFile", content, fileName, contentType);

        private static MultipartFormDataContent VideoForm() =>
            Form("videoFile", [0x00, 0x00, 0x00, 0x18], "clip.mp4", Mp4ContentType);

        private static MultipartFormDataContent Form(string field, byte[] content, string fileName, string contentType)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            return new MultipartFormDataContent { { file, field, fileName } };
        }

        private static async Task AssertRefusedAsync(HttpResponseMessage response, string message)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(message, (await response.Content.ReadFromJsonAsync<MessageResponse>())!.Message);
        }

        private IEnumerable<string> UploadedFiles(string userId)
        {
            var uploadPath = Factory.Services.GetRequiredService<IOptions<UploadSettings>>().Value.UploadPath;
            var userDirectory = Path.Combine(uploadPath, userId);

            return Directory.Exists(userDirectory) ? Directory.GetFiles(userDirectory) : [];
        }
    }
}
