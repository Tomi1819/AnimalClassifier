namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Constants.MessageConstants;
    using static AnimalClassifier.Core.Constants.RoleConstants;

    public class AccountControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string NewPassword = "secret2";
        private const string WrongPassword = "not-the-password";
        private const string AccountPath = "/api/account";
        private const string ChangePasswordPath = "/api/account/change-password";
        private const string SignOutOtherSessionsPath = "/api/account/sign-out-other-sessions";
        private const string HistoryPath = "/api/upload/history";

        private readonly ApiFactory factory;

        public AccountControllerTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task ChangePassword_WithoutSigningIn_IsRefused()
        {
            var response = await ChangePasswordAsync(factory.CreateClient(), Password, NewPassword);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithTheCurrentPassword_ChangesIt()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, Password, NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, NewPassword)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_EndsTheOtherSessions()
        {
            var account = await RegisterAsync();
            var elsewhere = await SignInAsync(account.Email, Password);
            var client = await SignInAsync(account.Email, Password);

            (await ChangePasswordAsync(client, Password, NewPassword)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await elsewhere.GetAsync(HistoryPath)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_AnswersWithATokenThatKeepsTheCallerSignedIn()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, Password, NewPassword);
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.Equal(HttpStatusCode.OK, (await WithToken(login!.Token).GetAsync(HistoryPath)).StatusCode);
        }

        // Anything but a bad request would be taken by the frontend as the
        // session itself having ended.
        [Fact]
        public async Task ChangePassword_WithAWrongCurrentPassword_IsABadRequest()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, WrongPassword, NewPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(IncorrectCurrentPassword, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithWrongCurrentPasswords_LocksTheAccount()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            await GuessUntilLockedOutAsync(client);

            var response = await LogInAsync(account.Email, Password);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains(LockedOutAccount, await response.Content.ReadAsStringAsync());
        }

        // Otherwise the lockout would stop nothing: guessing would carry on
        // here until the right password got through.
        [Fact]
        public async Task ChangePassword_WhileLocked_RefusesEvenTheRightPassword()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            await GuessUntilLockedOutAsync(client);

            var response = await ChangePasswordAsync(client, Password, NewPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(LockedOutAccount, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ChangePassword_WithATooShortPassword_IsRefused()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, Password, "abc");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_WithoutSigningIn_IsRefused()
        {
            var response = await SignOutOtherSessionsAsync(factory.CreateClient());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_EndsTheOtherSessions()
        {
            var account = await RegisterAsync();
            var elsewhere = await SignInAsync(account.Email, Password);
            var client = await SignInAsync(account.Email, Password);

            (await SignOutOtherSessionsAsync(client)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await elsewhere.GetAsync(HistoryPath)).StatusCode);
        }

        // The caller's old token goes with the rest, which is why the answer
        // carries a new one.
        [Fact]
        public async Task SignOutOtherSessions_EndsTheCallersOldToken()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            (await SignOutOtherSessionsAsync(client)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(HistoryPath)).StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_AnswersWithATokenThatKeepsTheCallerSignedIn()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await SignOutOtherSessionsAsync(client);
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.Equal(HttpStatusCode.OK, (await WithToken(login!.Token).GetAsync(HistoryPath)).StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_LeavesThePasswordAsItWas()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            (await SignOutOtherSessionsAsync(client)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_WithoutSigningIn_IsRefused()
        {
            var response = await DeleteAccountAsync(factory.CreateClient(), Password);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_WithThePassword_DeletesIt()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await DeleteAccountAsync(client, Password);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_EndsEverySession()
        {
            var account = await RegisterAsync();
            var elsewhere = await SignInAsync(account.Email, Password);
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await elsewhere.GetAsync(HistoryPath)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(HistoryPath)).StatusCode);
        }

        // As with changing the password, anything but a bad request would be
        // taken by the frontend as the session itself having ended.
        [Fact]
        public async Task DeleteAccount_WithAWrongPassword_IsABadRequest()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await DeleteAccountAsync(client, WrongPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(IncorrectCurrentPassword, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_ForAnAdministrator_IsRefused()
        {
            var account = await RegisterAsync();
            await MakeAdministratorAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            var response = await DeleteAccountAsync(client, Password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(AdministratorAccountDeletion, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        // Cleared recognitions only leave the owner's history, so they have to
        // go as well for the account to leave nothing behind.
        [Fact]
        public async Task DeleteAccount_RemovesTheRecognitions()
        {
            var account = await RegisterAsync();
            await AddRecognitionAsync(account.UserId, isDeleted: false);
            await AddRecognitionAsync(account.UserId, isDeleted: true);
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();
            Assert.False(await context.AnimalRecognitionLogs.AnyAsync(l => l.UserId == account.UserId));
        }

        [Fact]
        public async Task DeleteAccount_RemovesTheUploadedFiles()
        {
            var account = await RegisterAsync();
            var uploadDirectory = await AddUploadAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            Assert.False(Directory.Exists(uploadDirectory));
        }

        // Files cannot be brought back the way the database rows can, so a
        // refused request must not touch them.
        [Fact]
        public async Task DeleteAccount_WithAWrongPassword_KeepsTheUploadedFiles()
        {
            var account = await RegisterAsync();
            var uploadDirectory = await AddUploadAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            await DeleteAccountAsync(client, WrongPassword);

            Assert.True(Directory.Exists(uploadDirectory));
        }

        [Fact]
        public async Task DeleteAccount_KeepsTheAuditLogEntriesAboutIt()
        {
            var admin = await RegisterAsync();
            await MakeAdministratorAsync(admin.UserId);
            var adminClient = await SignInAsync(admin.Email, Password);
            var account = await RegisterAsync();
            (await adminClient.PostAsync($"/api/admin/users/{account.UserId}/lock", null)).EnsureSuccessStatusCode();
            (await adminClient.PostAsync($"/api/admin/users/{account.UserId}/unlock", null)).EnsureSuccessStatusCode();
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            var log = await adminClient.GetFromJsonAsync<PagedResult<AdminAuditLogItem>>("/api/admin/audit");
            var entries = log!.Items.Where(e => e.AdminEmail == admin.Email).ToList();
            Assert.Equal(2, entries.Count);
            Assert.All(entries, e => Assert.Equal(DeletedUser, e.UserEmail));
        }

        private async Task GuessUntilLockedOutAsync(HttpClient client)
        {
            var maxFailedAccessAttempts = factory.Services
                .GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;

            for (var attempt = 0; attempt < maxFailedAccessAttempts; attempt++)
            {
                await ChangePasswordAsync(client, WrongPassword, NewPassword);
            }
        }

        private static Task<HttpResponseMessage> ChangePasswordAsync(HttpClient client, string currentPassword, string newPassword) =>
            client.PostAsJsonAsync(ChangePasswordPath, new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

        private static Task<HttpResponseMessage> SignOutOtherSessionsAsync(HttpClient client) =>
            client.PostAsync(SignOutOtherSessionsPath, content: null);

        // HttpClient has no DeleteAsJsonAsync, and the password goes in the body.
        private static Task<HttpResponseMessage> DeleteAccountAsync(HttpClient client, string password) =>
            client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, AccountPath)
            {
                Content = JsonContent.Create(new DeleteAccountRequest { Password = password })
            });

        private async Task MakeAdministratorAsync(string userId)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await userManager.AddToRoleAsync((await userManager.FindByIdAsync(userId))!, Admin);
        }

        // Added directly, since uploading would need the recognition model.
        private async Task AddRecognitionAsync(string userId, bool isDeleted)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            context.AnimalRecognitionLogs.Add(new AnimalRecognitionLog
            {
                AnimalName = "Cat",
                ImagePath = $"/uploads/{userId}/cat.jpg",
                DateRecognized = DateTime.UtcNow,
                UserId = userId,
                IsDeleted = isDeleted
            });
            await context.SaveChangesAsync();
        }

        /// <returns>The directory the user's uploads are kept in.</returns>
        private async Task<string> AddUploadAsync(string userId)
        {
            var uploadPath = factory.Services.GetRequiredService<IOptions<UploadSettings>>().Value.UploadPath;
            var userDirectory = Path.Combine(uploadPath, userId);

            Directory.CreateDirectory(userDirectory);
            await File.WriteAllBytesAsync(Path.Combine(userDirectory, "cat.jpg"), [0xFF, 0xD8]);

            return userDirectory;
        }

        private Task<HttpResponseMessage> LogInAsync(string email, string password) =>
            factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LogInRequest
            {
                Email = email,
                Password = password
            });

        private async Task<HttpClient> SignInAsync(string email, string password)
        {
            var response = await LogInAsync(email, password);
            response.EnsureSuccessStatusCode();
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            return WithToken(login!.Token);
        }

        private HttpClient WithToken(string token)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        private async Task<RegisterResponse> RegisterAsync()
        {
            var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
            {
                FullName = "Test User",
                Email = UniqueEmail(),
                Password = Password
            });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
        }

        private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.test";
    }
}
