namespace AnimalClassifier.Tests.Admin
{
    using AnimalClassifier.Core.Admin.Models;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Recognitions.History.Models;
    using AnimalClassifier.Tests.Support;
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;

    public class AdminControllerTests : ApiTest
    {
        private const string UsersPath = "/api/admin/users";

        public AdminControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
        {
            var response = await Factory.CreateClient().GetAsync(UsersPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_AsUser_ReturnsForbidden()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync(UsersPath);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_AsAdmin_FindsUserByEmail()
        {
            var admin = await SignInAsync((await RegisterAdminAsync()).Email);
            var account = await RegisterAsync();

            var result = await admin.GetFromJsonAsync<PagedResult<AdminUserItem>>($"{UsersPath}?search={account.Email}");

            var user = Assert.Single(result!.Items);
            Assert.Equal(account.UserId, user.Id);
            Assert.False(user.IsAdmin);
            Assert.False(user.IsLocked);
        }

        [Fact]
        public async Task GetUserHistory_AsAdmin_ListsThatUsersHistory()
        {
            var admin = await SignInAsync((await RegisterAdminAsync()).Email);
            var account = await RegisterAsync();
            var recognition = await AddRecognitionAsync(account.UserId);

            var result = await admin.GetFromJsonAsync<PagedResult<RecognitionHistoryItem>>($"{UsersPath}/{account.UserId}/history");

            Assert.Equal(1, result!.TotalCount);
            Assert.Equal(recognition.Id, Assert.Single(result.Items).Id);
        }

        // So far past the last that the items before it number more than an
        // int holds.
        [Theory]
        [InlineData(UsersPath)]
        [InlineData(UsersPath + "/{0}/history")]
        [InlineData("/api/admin/audit")]
        public async Task List_OnTheLastPagePossible_IsEmpty(string path)
        {
            var account = await RegisterAdminAsync();
            var admin = await SignInAsync(account.Email);

            var result = await admin.GetFromJsonAsync<PagedResult<object>>($"{string.Format(CultureInfo.InvariantCulture, path, account.UserId)}?page={int.MaxValue}");

            Assert.Empty(result!.Items);
        }

        [Fact]
        public async Task LockUser_EndsSessionAndBlocksSignIn()
        {
            var admin = await SignInAsync((await RegisterAdminAsync()).Email);
            var account = await RegisterAsync();
            var user = await SignInAsync(account.Email);

            var response = await admin.PostAsync($"{UsersPath}/{account.UserId}/lock", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await user.GetAsync("/api/upload/history")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(account.Email)).StatusCode);
        }

        [Fact]
        public async Task UnlockUser_RestoresSignIn()
        {
            var admin = await SignInAsync((await RegisterAdminAsync()).Email);
            var account = await RegisterAsync();
            (await admin.PostAsync($"{UsersPath}/{account.UserId}/lock", null)).EnsureSuccessStatusCode();

            var response = await admin.PostAsync($"{UsersPath}/{account.UserId}/unlock", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email)).StatusCode);
        }

        [Fact]
        public async Task UnlockUser_NotLocked_ReturnsBadRequest()
        {
            var admin = await SignInAsync((await RegisterAdminAsync()).Email);
            var account = await RegisterAsync();

            var response = await admin.PostAsync($"{UsersPath}/{account.UserId}/unlock", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RevokeAdmin_EndsAdminAccess()
        {
            var admin = await SignInAsync((await RegisterAdminAsync()).Email);
            var account = await RegisterAdminAsync();
            var otherAdmin = await SignInAsync(account.Email);

            var response = await admin.PostAsync($"{UsersPath}/{account.UserId}/revoke-admin", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await otherAdmin.GetAsync(UsersPath)).StatusCode);

            var signedInAgain = await SignInAsync(account.Email);
            Assert.Equal(HttpStatusCode.Forbidden, (await signedInAgain.GetAsync(UsersPath)).StatusCode);
        }

        [Fact]
        public async Task LockUser_OwnAccount_ReturnsBadRequest()
        {
            var account = await RegisterAdminAsync();
            var admin = await SignInAsync(account.Email);

            var response = await admin.PostAsync($"{UsersPath}/{account.UserId}/lock", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GrantAdmin_IsRecordedInAuditLog()
        {
            var adminAccount = await RegisterAdminAsync();
            var admin = await SignInAsync(adminAccount.Email);
            var account = await RegisterAsync();

            var response = await admin.PostAsync($"{UsersPath}/{account.UserId}/grant-admin", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var log = await admin.GetFromJsonAsync<PagedResult<AdminAuditLogItem>>("/api/admin/audit");
            var latest = log!.Items.First();
            Assert.Equal(nameof(AdminAction.GrantAdmin), latest.Action);
            Assert.Equal(adminAccount.Email, latest.AdminEmail);
            Assert.Equal(account.Email, latest.UserEmail);
        }

        private async Task<RegisterResponse> RegisterAdminAsync()
        {
            var account = await RegisterAsync();
            await MakeAdministratorAsync(account.UserId);

            return account;
        }
    }
}
