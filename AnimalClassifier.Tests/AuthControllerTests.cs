namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.DTO;
    using Microsoft.AspNetCore.WebUtilities;
    using System.Net;
    using System.Net.Http.Json;

    public class AuthControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string ForgotPasswordPath = "/api/auth/forgot-password";

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
