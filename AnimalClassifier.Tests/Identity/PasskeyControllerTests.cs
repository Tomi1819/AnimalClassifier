namespace AnimalClassifier.Tests.Identity
{
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.Passkeys.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json.Nodes;
    using static AnimalClassifier.Core.Identity.Authentication.AuthenticationMessages;
    using static AnimalClassifier.Core.Identity.Passkeys.PasskeyMessages;
    using static AnimalClassifier.Core.Identity.Passwords.PasswordMessages;
    using static AnimalClassifier.Core.Identity.SecurityAlerts.SecurityAlertEmail;

    public class PasskeyControllerTests : ApiTest
    {
        private const string PasskeyPath = "/api/passkey";
        private const string OptionsPath = "/api/passkey/options";
        private const string SignInOptionsPath = "/api/auth/passkey/options";
        private const string SignInPath = "/api/auth/passkey/login";
        private const string HistoryPath = "/api/upload/history";

        public PasskeyControllerTests(ApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task Options_WithoutSigningIn_AreRefused()
        {
            var response = await Factory.CreateClient().PostAsync(OptionsPath, content: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Options_CarryTheOptionsAndAState()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);

            var options = await RequestOptionsAsync(client);

            Assert.NotNull(options.Options);
            Assert.False(string.IsNullOrWhiteSpace(options.State));
        }

        /// <summary>
        /// A passkey outlasts the session that adds it, and a password change
        /// leaves it in place, so a session alone must not be enough to add
        /// one.
        /// </summary>
        [Theory]
        [InlineData(WrongPassword)]
        [InlineData("")]
        public async Task Options_WithoutTheAccountsPassword_AreRefused(string password)
        {
            var client = await SignInAsync((await RegisterAsync()).Email);

            var response = await PostOptionsAsync(client, password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(IncorrectCurrentPassword, await response.Content.ReadAsStringAsync());
        }

        // Otherwise a session left open could be used to guess the password
        // here, one wrong answer after another.
        [Fact]
        public async Task Options_BeyondTheAttemptLimit_AreRefused()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);
            var permitLimit = Factory.Services
                .GetRequiredService<IOptions<PasswordConfirmationSettings>>().Value.PasswordConfirmationPermitLimit;

            for (var attempt = 0; attempt < permitLimit; attempt++)
            {
                await PostOptionsAsync(client, WrongPassword);
            }

            var response = await PostOptionsAsync(client, Password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(TooManyPasswordAttempts, await response.Content.ReadAsStringAsync());
        }

        // Anyone can lock an account out from outside by getting its password
        // wrong, which must not keep a signed-in owner from adding a passkey:
        // the way of signing in that a wrong password cannot touch.
        [Fact]
        public async Task Options_WhileSigningInIsLockedOut_AreStillGiven()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email);
            await LockOutSigningInAsync(account.Email);

            var response = await PostOptionsAsync(client, Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// A passkey is offered back only to the domain it was made for, and
        /// the browser takes that to be the one showing the page. Binding them
        /// to this API instead would leave the frontend unable to use any of
        /// them.
        /// </summary>
        [Fact]
        public async Task Options_BindThePasskeyToTheFrontendDomain()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);

            var options = await RequestOptionsAsync(client);

            Assert.Equal(ApiFactory.FrontendDomain, options.Options!["rp"]!["id"]!.GetValue<string>());
        }

        /// <summary>
        /// Signing in offers no account to start from, so the browser has to
        /// be holding credentials it can list without being told which.
        /// </summary>
        [Fact]
        public async Task Options_AskForADiscoverableCredential()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);

            var options = await RequestOptionsAsync(client);

            Assert.Equal(
                "required",
                options.Options!["authenticatorSelection"]!["residentKey"]!.GetValue<string>());
        }

        [Fact]
        public async Task Passkeys_ForANewAccount_AreEmpty()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);

            Assert.Empty(await ReadPasskeysAsync(client));
        }

        [Fact]
        public async Task Register_KeepsThePasskeyUnderItsName()
        {
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler());
            var client = await SignInAsync(app, (await RegisterAsync()).Email);

            var response = await RegisterPasskeyAsync(client, await RequestOptionsAsync(client), "Laptop");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var passkey = Assert.Single(await ReadPasskeysAsync(client));
            Assert.Equal("Laptop", passkey.Name);
        }

        [Fact]
        public async Task Register_EmailsASecurityAlert()
        {
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler());
            var account = await RegisterAsync();
            var client = await SignInAsync(app, account.Email);

            (await RegisterPasskeyAsync(client, await RequestOptionsAsync(client), "Laptop")).EnsureSuccessStatusCode();

            Assert.Contains(PasskeyAddedSubject, Factory.Emails.SubjectsSentTo(account.Email));
        }

        [Fact]
        public async Task Register_WithoutAName_NamesThePasskey()
        {
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler());
            var client = await SignInAsync(app, (await RegisterAsync()).Email);

            await RegisterPasskeyAsync(client, await RequestOptionsAsync(client), name: null);

            var passkey = Assert.Single(await ReadPasskeysAsync(client));
            Assert.False(string.IsNullOrWhiteSpace(passkey.Name));
        }

        /// <summary>
        /// The state is the only record of the account a new passkey belongs
        /// to. One naming somebody else must not be taken at its word, or an
        /// authenticator ends up registered against an account whoever owns it
        /// never had access to.
        /// </summary>
        [Fact]
        public async Task Register_CreditedToAnotherAccount_IsRefused()
        {
            var victim = await RegisterAsync();
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler { AttestationUserId = victim.UserId });

            var attacker = await SignInAsync(app, (await RegisterAsync()).Email);

            var response = await RegisterPasskeyAsync(attacker, await RequestOptionsAsync(attacker), "Mine");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(await ReadPasskeysAsync(attacker));
        }

        /// <summary>
        /// Each ceremony's state is protected under its own purpose, so one
        /// cannot be spent on the other.
        /// </summary>
        [Fact]
        public async Task Register_WithAStateFromTheSignInCeremony_IsRefused()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);
            var signInOptions = await RequestSignInOptionsAsync(Factory);

            var response = await RegisterPasskeyAsync(client, signInOptions, "Mixed up");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(ExpiredPasskeyCeremony, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Register_WithAMadeUpState_IsRefused()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);

            var response = await RegisterPasskeyAsync(
                client, new PasskeyOptionsResponse { State = "not-a-state" }, "Forged");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SignIn_WithAPasskey_OpensASession()
        {
            var account = await RegisterAsync();
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler { AssertionUserId = account.UserId });

            var response = await SignInWithPasskeyAsync(app);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var session = await ReadAsync<LoginResponse>(response);
            Assert.False(string.IsNullOrWhiteSpace(session.Token));

            // The session has to be worth what a password one is.
            var client = WithToken(app, session.Token);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(HistoryPath)).StatusCode);
        }

        /// <summary>
        /// Identity checks the credential, not the standing of the account
        /// behind it, so a locked user would otherwise sign straight back in.
        /// </summary>
        [Fact]
        public async Task SignIn_WithAPasskey_ForALockedAccount_IsRefused()
        {
            var account = await RegisterAsync();
            await LockAsync(account.UserId);

            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler { AssertionUserId = account.UserId });

            var response = await SignInWithPasskeyAsync(app);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains(LockedOutAccount, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SignIn_WithAnUnknownPasskey_IsRefused()
        {
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler());

            var response = await SignInWithPasskeyAsync(app);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Remove_TakesThePasskeyAway()
        {
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler());
            var client = await SignInAsync(app, (await RegisterAsync()).Email);

            await RegisterPasskeyAsync(client, await RequestOptionsAsync(client), "Laptop");
            var passkey = Assert.Single(await ReadPasskeysAsync(client));

            var response = await client.DeleteAsync($"{PasskeyPath}/{passkey.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await ReadPasskeysAsync(client));
        }

        [Fact]
        public async Task Remove_EmailsASecurityAlert()
        {
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler());
            var account = await RegisterAsync();
            var client = await SignInAsync(app, account.Email);

            await RegisterPasskeyAsync(client, await RequestOptionsAsync(client), "Laptop");
            var passkey = Assert.Single(await ReadPasskeysAsync(client));

            (await client.DeleteAsync($"{PasskeyPath}/{passkey.Id}")).EnsureSuccessStatusCode();

            Assert.Contains(PasskeyRemovedSubject, Factory.Emails.SubjectsSentTo(account.Email));
        }

        /// <summary>
        /// The id is the only thing naming a passkey, so knowing one must not
        /// be enough to take somebody else's away from them.
        /// </summary>
        [Fact]
        public async Task Remove_SomebodyElsesPasskey_IsNotFound()
        {
            using var app = Factory.WithPasskeyHandler(new StubPasskeyHandler());

            var owner = await SignInAsync(app, (await RegisterAsync()).Email);
            await RegisterPasskeyAsync(owner, await RequestOptionsAsync(owner), "Laptop");
            var passkey = Assert.Single(await ReadPasskeysAsync(owner));

            var stranger = await SignInAsync(app, (await RegisterAsync()).Email);
            var response = await stranger.DeleteAsync($"{PasskeyPath}/{passkey.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Single(await ReadPasskeysAsync(owner));
        }

        [Fact]
        public async Task Remove_AnUnknownPasskey_IsNotFound()
        {
            var client = await SignInAsync((await RegisterAsync()).Email);

            var response = await client.DeleteAsync($"{PasskeyPath}/bm90LWEtcGFzc2tleQ");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static async Task<HttpResponseMessage> SignInWithPasskeyAsync(WebApplicationFactory<Program> app)
        {
            var options = await RequestSignInOptionsAsync(app);

            return await app.CreateClient().PostAsJsonAsync(SignInPath, new PasskeyCredentialRequest
            {
                Credential = JsonNode.Parse("{}"),
                State = options.State
            });
        }

        private static async Task<PasskeyOptionsResponse> RequestSignInOptionsAsync(WebApplicationFactory<Program> app) =>
            await ReadAsync<PasskeyOptionsResponse>(
                await app.CreateClient().PostAsync(SignInOptionsPath, content: null));

        private static async Task<PasskeyOptionsResponse> RequestOptionsAsync(HttpClient client) =>
            await ReadAsync<PasskeyOptionsResponse>(await PostOptionsAsync(client, Password));

        private static Task<HttpResponseMessage> PostOptionsAsync(HttpClient client, string password) =>
            client.PostAsJsonAsync(OptionsPath, new PasskeyRegistrationOptionsRequest { Password = password });

        private static Task<HttpResponseMessage> RegisterPasskeyAsync(HttpClient client, PasskeyOptionsResponse options, string? name) =>
            client.PostAsJsonAsync(PasskeyPath, new PasskeyRegistrationRequest
            {
                Credential = JsonNode.Parse("{}"),
                State = options.State,
                Name = name
            });

        private static async Task<IReadOnlyList<PasskeySummary>> ReadPasskeysAsync(HttpClient client) =>
            await ReadAsync<List<PasskeySummary>>(await client.GetAsync(PasskeyPath));

        private async Task LockAsync(string userId)
        {
            using var scope = Factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByIdAsync(userId);
            await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.MaxValue);
        }
    }
}
