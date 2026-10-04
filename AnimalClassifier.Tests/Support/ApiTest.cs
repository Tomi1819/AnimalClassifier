namespace AnimalClassifier.Tests.Support
{
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.EmailConfirmation.Models;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Identity.Authentication.AuthenticationMessages;
    using static AnimalClassifier.Core.Identity.RoleConstants;

    /// <summary>
    /// What the tests of every controller start from: an account, and a client
    /// signed in to it. Each test class runs against an app and a database of
    /// its own.
    /// </summary>
    public abstract class ApiTest : IClassFixture<ApiFactory>
    {
        protected const string Password = "secret-one";
        protected const string WrongPassword = "not-the-password";

        private const string RegisterPath = "/api/auth/register";
        private const string LoginPath = "/api/auth/login";
        private const string ConfirmEmailPath = "/api/auth/confirm-email";
        private const string FullName = "Test User";
        private const string BearerScheme = "Bearer";

        protected ApiTest(ApiFactory factory)
        {
            Factory = factory;
        }

        protected ApiFactory Factory { get; }

        protected static string UniqueEmail() => $"{Guid.NewGuid():N}@example.test";

        protected async Task<RegisterResponse> RegisterAsync() =>
            await ReadAsync<RegisterResponse>(await RegisterAsync(UniqueEmail(), Password));

        protected Task<HttpResponseMessage> RegisterAsync(string email, string password, string fullName = FullName) =>
            Factory.CreateClient().PostAsJsonAsync(RegisterPath, new RegisterRequest
            {
                FullName = fullName,
                Email = email,
                Password = password
            });

        protected Task<HttpResponseMessage> LogInAsync(string email, string password = Password) =>
            Factory.CreateClient().PostAsJsonAsync(LoginPath, new LoginRequest
            {
                Email = email,
                Password = password
            });

        protected async Task<HttpClient> SignInAsync(string email, string password = Password) =>
            await SignInAsync(Factory, email, password);

        /// <summary>
        /// Signs in to a copy of the app, such as one with a setting changed.
        /// The copy shares the database and the signing key, so the token the
        /// usual one issues is good for it as well.
        /// </summary>
        protected async Task<HttpClient> SignInAsync(WebApplicationFactory<Program> app, string email, string password = Password)
        {
            var login = await ReadAsync<LoginResponse>(await LogInAsync(email, password));

            return WithToken(app, login.Token);
        }

        /// <summary>
        /// The token in the last link emailed to the address, as the
        /// frontend's page would read it.
        /// </summary>
        protected string TokenSentTo(string email)
        {
            var link = Factory.Emails.LinkSentTo(email);
            Assert.NotNull(link);

            return QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();
        }

        protected Task<HttpResponseMessage> ConfirmEmailAsync(string email, string token) =>
            Factory.CreateClient().PostAsJsonAsync(ConfirmEmailPath, new ConfirmEmailRequest
            {
                Email = email,
                Token = token
            });

        /// <summary>
        /// Confirms the email with the link registering mailed to it.
        /// </summary>
        protected async Task ConfirmEmailAsync(string email) =>
            (await ConfirmEmailAsync(email, TokenSentTo(email))).EnsureSuccessStatusCode();

        protected HttpClient WithToken(string token) => WithToken(Factory, token);

        protected static HttpClient WithToken(WebApplicationFactory<Program> app, string token)
        {
            var client = app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);

            return client;
        }

        protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        /// <summary>
        /// Adds a recognition straight to the database, as an upload would
        /// leave one, without the file or the model an upload needs.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file it was made from, which is one of its own
        /// unless given.
        /// </param>
        protected async Task<AnimalRecognitionLog> AddRecognitionAsync(
            string userId,
            string animalName = "Cat",
            string? fileName = null,
            DateTime? dateRecognized = null,
            bool isCleared = false)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            var log = new AnimalRecognitionLog
            {
                AnimalName = animalName,
                ImagePath = $"/uploads/{userId}/{fileName ?? $"{Guid.NewGuid():N}.jpg"}",
                DateRecognized = dateRecognized ?? DateTime.UtcNow,
                UserId = userId,
                IsDeleted = isCleared
            };

            context.AnimalRecognitionLogs.Add(log);
            await context.SaveChangesAsync();

            return log;
        }

        protected async Task MakeAdministratorAsync(string userId)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await userManager.AddToRoleAsync((await userManager.FindByIdAsync(userId))!, Admin);
        }

        // The way anyone could from outside: by getting the password wrong
        // until signing in is refused.
        protected async Task LockOutSigningInAsync(string email)
        {
            var maxFailedAccessAttempts = Factory.Services
                .GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;

            for (var attempt = 0; attempt < maxFailedAccessAttempts; attempt++)
            {
                await LogInAsync(email, WrongPassword);
            }

            Assert.Contains(LockedOutAccount, await (await LogInAsync(email)).Content.ReadAsStringAsync());
        }
    }
}
