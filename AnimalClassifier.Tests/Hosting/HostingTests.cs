namespace AnimalClassifier.Tests.Hosting
{
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.RateLimiting;
    using AnimalClassifier.Tests.Support;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net;
    using System.Net.Http.Json;

    public class HostingTests : ApiTest
    {
        private const string AccountPath = "/api/account";
        private const string LoginPath = "/api/auth/login";
        private const string ForwardedFor = "X-Forwarded-For";

        public HostingTests(ApiFactory factory)
            : base(factory)
        {
        }

        // A refusal included, whose headers are cleared on its way out.
        [Theory]
        [InlineData(AccountPath)]
        [InlineData("/api/media/not-a-link")]
        public async Task EveryAnswer_CarriesTheSecurityHeaders(string path)
        {
            var response = await Factory.CreateClient().GetAsync(path);

            Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
            Assert.Equal("default-src 'none'; frame-ancestors 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        }

        [Fact]
        public async Task AnAnswerOverHttps_HoldsTheBrowserToHttps()
        {
            var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://api.example.test") });

            var response = await client.GetAsync(AccountPath);

            Assert.Equal("max-age=31536000", Assert.Single(response.Headers.GetValues("Strict-Transport-Security")));
        }

        // Every request would otherwise come from the proxy, and share one
        // allowance between every caller.
        [Fact]
        public async Task BehindAProxyOnThisMachine_EachCallerIsLimitedByTheirOwnAddress()
        {
            using var app = WithLoginLimitOfOne(IPAddress.Loopback);
            var client = app.CreateClient();

            var first = await LogInFromAsync(client, "203.0.113.1");
            var again = await LogInFromAsync(client, "203.0.113.1");
            var someoneElse = await LogInFromAsync(client, "203.0.113.2");

            Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, again.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, someoneElse.StatusCode);
        }

        // Or anyone could slip every limit by naming a new address each time.
        [Fact]
        public async Task ACallerWhoIsNoKnownProxy_CannotChooseTheirAddress()
        {
            using var app = WithLoginLimitOfOne(IPAddress.Parse("198.51.100.7"));
            var client = app.CreateClient();

            await LogInFromAsync(client, "203.0.113.1");
            var response = await LogInFromAsync(client, "203.0.113.2");

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        private WebApplicationFactory<Program> WithLoginLimitOfOne(IPAddress callerAddress) =>
            Factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting(ApiFactory.Key<RateLimitSettings>(nameof(RateLimitSettings.LoginPermitLimit)), "1");
                builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new CallerAddress(callerAddress)));
            });

        private static Task<HttpResponseMessage> LogInFromAsync(HttpClient client, string forwardedFor)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
            {
                Content = JsonContent.Create(new LoginRequest { Email = UniqueEmail(), Password = WrongPassword })
            };
            request.Headers.Add(ForwardedFor, forwardedFor);

            return client.SendAsync(request);
        }

        // The test server's requests come from no address at all, so this
        // gives them the one a connection would have.
        private sealed class CallerAddress : IStartupFilter
        {
            private readonly IPAddress address;

            public CallerAddress(IPAddress address)
            {
                this.address = address;
            }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = address;

                    return nextMiddleware(context);
                });

                next(app);
            };
        }
    }
}
