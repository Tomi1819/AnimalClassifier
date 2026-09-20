namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.DTO;
    using Microsoft.AspNetCore.WebUtilities;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Constants.MessageConstants;

    public class AuthControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string NewPassword = "secret2";
        private const string ForgotPasswordPath = "/api/auth/forgot-password";
        private const string ResetPasswordPath = "/api/auth/reset-password";
        private const string HistoryPath = "/api/upload/history";

        private readonly ApiFactory factory;

        public AuthControllerTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task ForgotPassword_EmailsALinkCarryingTheAddressAndAToken()
        {
            var account = await RegisterAsync();

            var response = await ForgotPasswordAsync(account.Email);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var link = factory.Emails.LinkSentTo(account.Email);
            Assert.NotNull(link);

            var query = QueryHelpers.ParseQuery(new Uri(link).Query);
            Assert.Equal(account.Email, query["email"]);
            Assert.False(string.IsNullOrWhiteSpace(query["token"]));
        }

        [Fact]
        public async Task ForgotPassword_ForAnUnknownAddress_SendsNothing()
        {
            var unknown = UniqueEmail();

            var response = await ForgotPasswordAsync(unknown);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(factory.Emails.AnySentTo(unknown));
        }

        /// <summary>
        /// The whole point of the endpoint's answer: it must not become a way
        /// of asking which addresses are registered.
        /// </summary>
        [Fact]
        public async Task ForgotPassword_AnswersKnownAndUnknownAddressesAlike()
        {
            var account = await RegisterAsync();

            var known = await ForgotPasswordAsync(account.Email);
            var unknown = await ForgotPasswordAsync(UniqueEmail());

            Assert.Equal(known.StatusCode, unknown.StatusCode);
            Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ResetPassword_WithTheEmailedToken_ChangesThePassword()
        {
            var account = await RegisterAsync();
            var token = await RequestResetTokenAsync(account.Email);

            var response = await ResetPasswordAsync(account.Email, token, NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, NewPassword)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        /// <summary>
        /// Whoever knew the old password may be the reason for the reset, so
        /// the sessions they are holding have to end with it.
        /// </summary>
        [Fact]
        public async Task ResetPassword_EndsTheSessionsOpenedWithTheOldPassword()
        {
            var account = await RegisterAsync();
            var signedIn = await SignInAsync(account.Email, Password);
            Assert.Equal(HttpStatusCode.OK, (await signedIn.GetAsync(HistoryPath)).StatusCode);

            var token = await RequestResetTokenAsync(account.Email);
            (await ResetPasswordAsync(account.Email, token, NewPassword)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await signedIn.GetAsync(HistoryPath)).StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithTheSameTokenTwice_IsRefused()
        {
            var account = await RegisterAsync();
            var token = await RequestResetTokenAsync(account.Email);
            (await ResetPasswordAsync(account.Email, token, NewPassword)).EnsureSuccessStatusCode();

            var response = await ResetPasswordAsync(account.Email, token, "secret3");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, NewPassword)).StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithAnotherAccountsToken_IsRefused()
        {
            var account = await RegisterAsync();
            var other = await RegisterAsync();
            var othersToken = await RequestResetTokenAsync(other.Email);

            var response = await ResetPasswordAsync(account.Email, othersToken, NewPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithAMalformedToken_IsRefused()
        {
            var account = await RegisterAsync();

            var response = await ResetPasswordAsync(account.Email, "not-a-real-token", NewPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        /// <summary>
        /// A password the rules refuse is the one failure the user can act on,
        /// so it must not arrive dressed as a broken link.
        /// </summary>
        [Fact]
        public async Task ResetPassword_WithATooShortPassword_SaysSo()
        {
            var account = await RegisterAsync();
            var token = await RequestResetTokenAsync(account.Email);

            var response = await ResetPasswordAsync(account.Email, token, "abc");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain(InvalidPasswordResetLink, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        private async Task<string> RequestResetTokenAsync(string email)
        {
            (await ForgotPasswordAsync(email)).EnsureSuccessStatusCode();

            var link = factory.Emails.LinkSentTo(email);
            Assert.NotNull(link);

            return QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();
        }

        private Task<HttpResponseMessage> ResetPasswordAsync(string email, string token, string newPassword) =>
            factory.CreateClient().PostAsJsonAsync(ResetPasswordPath, new ResetPasswordRequest
            {
                Email = email,
                Token = token,
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

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

            return client;
        }

        private Task<HttpResponseMessage> ForgotPasswordAsync(string email) =>
            factory.CreateClient().PostAsJsonAsync(ForgotPasswordPath, new ForgotPasswordRequest { Email = email });

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
