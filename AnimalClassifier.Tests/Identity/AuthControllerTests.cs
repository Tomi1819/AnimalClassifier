namespace AnimalClassifier.Tests.Identity
{
    using AnimalClassifier.Core.Identity;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.EmailConfirmation;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Core.Identity.Passwords.Models;
    using AnimalClassifier.RateLimiting;
    using AnimalClassifier.Tests.Support;
    using Microsoft.AspNetCore.WebUtilities;
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Identity.Authentication.AuthenticationMessages;
    using static AnimalClassifier.Core.Identity.EmailConfirmation.EmailConfirmationMessages;
    using static AnimalClassifier.Core.Identity.IdentityMessages;
    using static AnimalClassifier.Core.Identity.Passwords.PasswordMessages;
    using static AnimalClassifier.Core.Identity.SecurityAlerts.SecurityAlertEmail;

    public class AuthControllerTests : ApiTest
    {
        private const string NewPassword = "secret-two";
        private const string LoginPath = "/api/auth/login";
        private const string ForgotPasswordPath = "/api/auth/forgot-password";
        private const string ResetPasswordPath = "/api/auth/reset-password";
        private const string HistoryPath = "/api/upload/history";
        private const string AccountPath = "/api/account";
        private const string RegisterPath = "/api/auth/register";

        public AuthControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        // Unlike a later change of name, which keeps the letters as typed.
        [Fact]
        public async Task Register_CapitalisesEachWordOfTheName()
        {
            var response = await RegisterWithNameAsync("  jane   mcDONALD ");
            var account = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Jane Mcdonald", account!.FullName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Register_WithoutAName_GivesTheAccountOne(string blankName)
        {
            var response = await RegisterWithNameAsync(blankName);
            var account = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(AccountName.Unknown, account!.FullName);
        }

        [Fact]
        public async Task Register_WithATooLongName_IsABadRequest()
        {
            var response = await RegisterWithNameAsync(new string('a', AccountName.MaxLength + 1));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, FullNameTooLong, AccountName.MaxLength), await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("@example.test")]
        public async Task Register_WithSomethingOtherThanAnEmail_IsABadRequest(string email)
        {
            var response = await RegisterAsync(email, Password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(email)).StatusCode);
        }

        [Fact]
        public async Task Register_WithATooLongEmail_IsABadRequest()
        {
            var email = $"{new string('a', AuthService.MaxEmailLength)}@example.test";

            var response = await RegisterAsync(email, Password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, EmailTooLong, AuthService.MaxEmailLength), await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Register_WithATooShortPassword_IsABadRequest()
        {
            var email = UniqueEmail();

            var response = await RegisterAsync(email, new string('a', PasswordPolicy.MinLength - 1));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(email, new string('a', PasswordPolicy.MinLength - 1))).StatusCode);
        }

        // Anyone trying to get in already knows the address, which makes it
        // the first guess, in whatever case it is typed.
        [Fact]
        public async Task Register_WithTheEmailAsThePassword_IsABadRequest()
        {
            var email = UniqueEmail();

            var response = await RegisterAsync(email, email.ToUpperInvariant());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(PasswordIsEmail, await response.Content.ReadAsStringAsync());
        }

        // Each registration mails an address the caller picks.
        [Fact]
        public async Task Register_BeyondTheLimit_IsRefused()
        {
            using var limited = Factory.WithWebHostBuilder(builder =>
                builder.UseSetting(ApiFactory.Key<RateLimitSettings>(nameof(RateLimitSettings.RegisterPermitLimit)), "1"));

            var client = limited.CreateClient();

            var allowed = await client.PostAsJsonAsync(RegisterPath, new RegisterRequest { Email = UniqueEmail(), Password = Password });
            var refused = await client.PostAsJsonAsync(RegisterPath, new RegisterRequest { Email = UniqueEmail(), Password = Password });

            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        }

        [Fact]
        public async Task Register_EmailsALinkToConfirmTheAddress()
        {
            var account = await RegisterAsync();

            Assert.Contains(EmailConfirmationEmail.Subject, Factory.Emails.SubjectsSentTo(account.Email));

            var link = Factory.Emails.LinkSentTo(account.Email);
            Assert.NotNull(link);
            Assert.Equal(account.Email, QueryHelpers.ParseQuery(new Uri(link).Query)["email"]);
        }

        [Fact]
        public async Task ConfirmEmail_WithTheEmailedToken_ConfirmsIt()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email);

            var response = await ConfirmEmailAsync(account.Email, TokenSentTo(account.Email));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True((await client.GetFromJsonAsync<AccountProfile>(AccountPath))!.EmailConfirmed);
        }

        [Fact]
        public async Task ConfirmEmail_WithAnotherAccountsToken_IsRefused()
        {
            var account = await RegisterAsync();
            var other = await RegisterAsync();
            var client = await SignInAsync(account.Email);

            var response = await ConfirmEmailAsync(account.Email, TokenSentTo(other.Email));

            await AssertInvalidConfirmationLinkAsync(response);
            Assert.False((await client.GetFromJsonAsync<AccountProfile>(AccountPath))!.EmailConfirmed);
        }

        [Fact]
        public async Task ConfirmEmail_WithAMalformedToken_IsRefused()
        {
            var account = await RegisterAsync();

            var response = await ConfirmEmailAsync(account.Email, "not-a-real-token");

            await AssertInvalidConfirmationLinkAsync(response);
        }

        // A lockout guards only the account a wrong password was tried on,
        // which stops nobody trying a few on every account from one address.
        [Fact]
        public async Task Login_BeyondTheLimit_IsRefused()
        {
            using var limited = Factory.WithWebHostBuilder(builder =>
                builder.UseSetting(ApiFactory.Key<RateLimitSettings>(nameof(RateLimitSettings.LoginPermitLimit)), "1"));

            var account = await RegisterAsync();
            var client = limited.CreateClient();
            var request = new LoginRequest { Email = account.Email, Password = Password };

            var allowed = await client.PostAsJsonAsync(LoginPath, request);
            var refused = await client.PostAsJsonAsync(LoginPath, request);

            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        }

        // An attempt that is turned away never reaches the password check,
        // so it cannot be used to lock the account either.
        [Fact]
        public async Task Login_BeyondTheLimit_DoesNotCountTowardsALockout()
        {
            using var limited = Factory.WithWebHostBuilder(builder =>
                builder.UseSetting(ApiFactory.Key<RateLimitSettings>(nameof(RateLimitSettings.LoginPermitLimit)), "1"));

            var account = await RegisterAsync();
            var client = limited.CreateClient();
            var wrongGuess = new LoginRequest { Email = account.Email, Password = "not-the-password" };

            for (var attempt = 0; attempt < 10; attempt++)
            {
                await client.PostAsJsonAsync(LoginPath, wrongGuess);
            }

            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_EmailsALinkCarryingTheAddressAndAToken()
        {
            var account = await RegisterAsync();

            var response = await ForgotPasswordAsync(account.Email);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var link = Factory.Emails.LinkSentTo(account.Email);
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
            Assert.False(Factory.Emails.AnySentTo(unknown));
        }

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
        public async Task ForgotPassword_BeyondTheLimit_IsRefused()
        {
            using var limited = Factory.WithWebHostBuilder(builder =>
                builder.UseSetting(ApiFactory.Key<RateLimitSettings>(nameof(RateLimitSettings.PasswordResetPermitLimit)), "1"));

            var client = limited.CreateClient();
            var request = new ForgotPasswordRequest { Email = UniqueEmail() };

            var allowed = await client.PostAsJsonAsync(ForgotPasswordPath, request);
            var refused = await client.PostAsJsonAsync(ForgotPasswordPath, request);

            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
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
        public async Task ResetPassword_EmailsASecurityAlert()
        {
            var account = await RegisterAsync();
            var token = await RequestResetTokenAsync(account.Email);

            (await ResetPasswordAsync(account.Email, token, NewPassword)).EnsureSuccessStatusCode();

            Assert.Contains(PasswordChangedSubject, Factory.Emails.SubjectsSentTo(account.Email));
        }

        [Fact]
        public async Task ResetPassword_WithTheSameTokenTwice_IsRefused()
        {
            var account = await RegisterAsync();
            var token = await RequestResetTokenAsync(account.Email);
            (await ResetPasswordAsync(account.Email, token, NewPassword)).EnsureSuccessStatusCode();

            var response = await ResetPasswordAsync(account.Email, token, "secret-three");

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

        // A password the rules refuse is the one failure the user can act on,
        // so it must not arrive dressed as a broken link.
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

            return TokenSentTo(email);
        }

        private static async Task AssertInvalidConfirmationLinkAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(InvalidEmailConfirmationLink, await response.Content.ReadAsStringAsync());
        }

        private Task<HttpResponseMessage> ResetPasswordAsync(string email, string token, string newPassword) =>
            Factory.CreateClient().PostAsJsonAsync(ResetPasswordPath, new ResetPasswordRequest
            {
                Email = email,
                Token = token,
                NewPassword = newPassword
            });

        private Task<HttpResponseMessage> ForgotPasswordAsync(string email) =>
            Factory.CreateClient().PostAsJsonAsync(ForgotPasswordPath, new ForgotPasswordRequest { Email = email });

        private Task<HttpResponseMessage> RegisterWithNameAsync(string fullName) =>
            RegisterAsync(UniqueEmail(), Password, fullName);
    }
}
