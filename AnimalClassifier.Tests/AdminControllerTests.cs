namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Constants.RoleConstants;

    public class AdminControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string UsersPath = "/api/admin/users";

        private readonly ApiFactory factory;

        public AdminControllerTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
        {
            var response = await factory.CreateClient().GetAsync(UsersPath);

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

        private async Task<RegisterResponse> RegisterAsync()
        {
            var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
            {
                FullName = "Test User",
                Email = $"{Guid.NewGuid():N}@example.test",
                Password = Password
            });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
        }

        private async Task<RegisterResponse> RegisterAdminAsync()
        {
            var account = await RegisterAsync();

            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await userManager.AddToRoleAsync((await userManager.FindByIdAsync(account.UserId))!, Admin);

            return account;
        }

        private async Task<HttpClient> SignInAsync(string email)
        {
            var response = await LogInAsync(email);
            response.EnsureSuccessStatusCode();
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
            return client;
        }

        private Task<HttpResponseMessage> LogInAsync(string email) =>
            factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LogInRequest { Email = email, Password = Password });
    }
}
