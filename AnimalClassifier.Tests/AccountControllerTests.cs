namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.DTO;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Constants.MessageConstants;

    public class AccountControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string NewPassword = "secret2";
        private const string WrongPassword = "not-the-password";
        private const string ChangePasswordPath = "/api/account/change-password";
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
