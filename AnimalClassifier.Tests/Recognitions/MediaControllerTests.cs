namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Recognitions.History.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Tests.Support;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Http.Json;

    public class MediaControllerTests : ApiTest
    {
        private const string HistoryPath = "/api/upload/history";

        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];

        public MediaControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        // An image tag sends no token, so the link alone has to be enough.
        [Fact]
        public async Task ALink_LoadsItsFile_WithoutSigningIn()
        {
            var account = await RegisterAsync();
            var link = await AddUploadAsync(account);

            var response = await Factory.CreateClient().GetAsync(link);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Jpeg, await response.Content.ReadAsByteArrayAsync());
            Assert.True(response.Headers.CacheControl?.Private);
        }

        [Fact]
        public async Task ALinkThatWasTamperedWith_IsNotFound()
        {
            var account = await RegisterAsync();
            var link = await AddUploadAsync(account);

            var middle = link.Length / 2;
            var tampered = link[..middle] + (link[middle] == 'A' ? 'B' : 'A') + link[(middle + 1)..];

            var response = await Factory.CreateClient().GetAsync(tampered);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // As once the account it belonged to has been deleted.
        [Fact]
        public async Task ALinkToAFileThatIsGone_IsNotFound()
        {
            var account = await RegisterAsync();
            var link = await AddUploadAsync(account);
            File.Delete(Directory.GetFiles(UserDirectory(account.UserId)).Single());

            var response = await Factory.CreateClient().GetAsync(link);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Where the uploads used to be served, to anyone who had the path.
        [Fact]
        public async Task TheUploadFolder_IsNotServed()
        {
            var account = await RegisterAsync();
            await AddUploadAsync(account);
            var fileName = Path.GetFileName(Directory.GetFiles(UserDirectory(account.UserId)).Single());

            var response = await Factory.CreateClient().GetAsync($"/uploads/{account.UserId}/{fileName}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <returns>The link the uploaded image is loaded by.</returns>
        private async Task<string> AddUploadAsync(RegisterResponse account)
        {
            var fileName = $"{Guid.NewGuid():N}.jpg";
            Directory.CreateDirectory(UserDirectory(account.UserId));
            await File.WriteAllBytesAsync(Path.Combine(UserDirectory(account.UserId), fileName), Jpeg);
            await AddRecognitionAsync(account.UserId, fileName: fileName);

            var user = await SignInAsync(account.Email);
            var item = Assert.Single((await user.GetFromJsonAsync<PagedResult<RecognitionHistoryItem>>(HistoryPath))!.Items);
            Assert.StartsWith($"/{MediaLinkService.Route}/", item.MediaPath);

            return item.MediaPath;
        }

        private string UserDirectory(string userId) =>
            Path.Combine(Factory.Services.GetRequiredService<IOptions<UploadSettings>>().Value.UploadPath, userId);
    }
}
