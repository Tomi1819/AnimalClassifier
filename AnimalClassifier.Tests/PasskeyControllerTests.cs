namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json.Nodes;
    using static AnimalClassifier.Core.Constants.MessageConstants;

    public class PasskeyControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string PasskeyPath = "/api/passkey";
        private const string OptionsPath = "/api/passkey/options";
        private const string SignInOptionsPath = "/api/auth/passkey/options";
        private const string SignInPath = "/api/auth/passkey/login";
        private const string HistoryPath = "/api/upload/history";

        private readonly ApiFactory factory;

        public PasskeyControllerTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task Options_WithoutSigningIn_AreRefused()
        {
            var response = await factory.CreateClient().PostAsync(OptionsPath, content: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Options_CarryTheOptionsAndAState()
        {
            var client = await SignInAsync(await RegisterAsync());

            var options = await RequestOptionsAsync(client);

            Assert.NotNull(options.Options);
            Assert.False(string.IsNullOrWhiteSpace(options.State));
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
            var client = await SignInAsync(await RegisterAsync());

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
            var client = await SignInAsync(await RegisterAsync());

            var options = await RequestOptionsAsync(client);

            Assert.Equal(
                "required",
                options.Options!["authenticatorSelection"]!["residentKey"]!.GetValue<string>());
        }

        [Fact]
        public async Task Passkeys_ForANewAccount_AreEmpty()
        {
            var client = await SignInAsync(await RegisterAsync());

            Assert.Empty(await ReadPasskeysAsync(client));
        }

        [Fact]
        public async Task Register_KeepsThePasskeyUnderItsName()
        {
            using var app = factory.WithPasskeyHandler(new StubPasskeyHandler());
            var client = await SignInAsync(await RegisterAsync(), app);

            var response = await RegisterPasskeyAsync(client, await RequestOptionsAsync(client), "Laptop");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var passkey = Assert.Single(await ReadPasskeysAsync(client));
            Assert.Equal("Laptop", passkey.Name);
        }

        [Fact]
        public async Task Register_WithoutAName_NamesThePasskey()
        {
            using var app = factory.WithPasskeyHandler(new StubPasskeyHandler());
            var client = await SignInAsync(await RegisterAsync(), app);

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
            using var app = factory.WithPasskeyHandler(new StubPasskeyHandler { AttestationUserId = victim.UserId });

            var attacker = await SignInAsync(await RegisterAsync(), app);

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
            var client = await SignInAsync(await RegisterAsync());
            var signInOptions = await RequestSignInOptionsAsync(factory);

            var response = await RegisterPasskeyAsync(client, signInOptions, "Mixed up");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(ExpiredPasskeyCeremony, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Register_WithAMadeUpState_IsRefused()
        {
            var client = await SignInAsync(await RegisterAsync());

            var response = await RegisterPasskeyAsync(
                client, new PasskeyOptionsResponse { State = "not-a-state" }, "Forged");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SignIn_WithAPasskey_OpensASession()
        {
            var account = await RegisterAsync();
            using var app = factory.WithPasskeyHandler(new StubPasskeyHandler { AssertionUserId = account.UserId });

            var response = await SignInWithPasskeyAsync(app);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var session = await ReadAsync<LoginResponse>(response);
            Assert.False(string.IsNullOrWhiteSpace(session.Token));

            // The session has to be worth what a password one is.
            var client = Authorize(app.CreateClient(), session.Token);
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

            using var app = factory.WithPasskeyHandler(new StubPasskeyHandler { AssertionUserId = account.UserId });

            var response = await SignInWithPasskeyAsync(app);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains(LockedOutAccount, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SignIn_WithAnUnknownPasskey_IsRefused()
        {
            using var app = factory.WithPasskeyHandler(new StubPasskeyHandler());

            var response = await SignInWithPasskeyAsync(app);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Remove_TakesThePasskeyAway()
        {
            using var app = factory.WithPasskeyHandler(new StubPasskeyHandler());
            var client = await SignInAsync(await RegisterAsync(), app);

            await RegisterPasskeyAsync(client, await RequestOptionsAsync(client), "Laptop");
            var passkey = Assert.Single(await ReadPasskeysAsync(client));

            var response = await client.DeleteAsync($"{PasskeyPath}/{passkey.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await ReadPasskeysAsync(client));
        }

        [Fact]
        public async Task Remove_AnUnknownPasskey_IsNotFound()
        {
            var client = await SignInAsync(await RegisterAsync());

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
            await ReadAsync<PasskeyOptionsResponse>(await client.PostAsync(OptionsPath, content: null));

        private static Task<HttpResponseMessage> RegisterPasskeyAsync(HttpClient client, PasskeyOptionsResponse options, string? name) =>
            client.PostAsJsonAsync(PasskeyPath, new PasskeyRegistrationRequest
            {
                Credential = JsonNode.Parse("{}"),
                State = options.State,
                Name = name
            });

        private static async Task<IReadOnlyList<PasskeySummary>> ReadPasskeysAsync(HttpClient client) =>
            await ReadAsync<List<PasskeySummary>>(await client.GetAsync(PasskeyPath));

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private static HttpClient Authorize(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        private async Task LockAsync(string userId)
        {
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByIdAsync(userId);
            await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.MaxValue);
        }

        private async Task<HttpClient> SignInAsync(RegisterResponse account, WebApplicationFactory<Program>? app = null)
        {
            app ??= factory;

            var response = await app.CreateClient().PostAsJsonAsync("/api/auth/login", new LogInRequest
            {
                Email = account.Email,
                Password = Password
            });
            response.EnsureSuccessStatusCode();

            var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;

            return Authorize(app.CreateClient(), login.Token);
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
    }
}
