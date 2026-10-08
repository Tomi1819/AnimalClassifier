namespace AnimalClassifier.Tests.Settings
{
    using AnimalClassifier.Core.Common.Settings;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Hosting;
    using AnimalClassifier.Infrastructure.Classification;
    using AnimalClassifier.Infrastructure.Storage;
    using AnimalClassifier.RateLimiting;
    using AnimalClassifier.Tests.Support;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The app refuses to start while a setting it needs is missing or wrong,
    /// rather than failing later on whichever request first needs it.
    /// </summary>
    public class SettingsTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory factory;

        public SettingsTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        public static TheoryData<string, string> BrokenSettings => new()
        {
            // Too short to sign with.
            { ApiFactory.Key<JwtSettings>(nameof(JwtSettings.SecretKey)), "too-short" },

            // Resolved, it would be the content root, where the app itself is.
            { ApiFactory.Key<UploadSettings>(nameof(UploadSettings.UploadPath)), "" },

            // A new deployment may replace the app's folder whole, and
            // everything kept in it.
            { ApiFactory.Key<UploadSettings>(nameof(UploadSettings.UploadPath)), "App_Data/uploads" },
            { ApiFactory.Key<DataProtectionSettings>(nameof(DataProtectionSettings.KeysPath)), "App_Data/keys" },

            // Without it every link and passkey ceremony in flight dies with
            // each restart.
            { ApiFactory.Key<DataProtectionSettings>(nameof(DataProtectionSettings.KeysPath)), "" },

            { $"{ApiFactory.Key<ForwardedHeadersSettings>(nameof(ForwardedHeadersSettings.KnownProxies))}:0", "not-an-address" },

            // The emailed links and the passkeys' domain are made from it.
            { ApiFactory.Key<FrontendSettings>(nameof(FrontendSettings.BaseUrl)), "" },

            { ApiFactory.Key<MLModelSettings>(nameof(MLModelSettings.Path)), "" },
            { ApiFactory.Key<RateLimitSettings>(nameof(RateLimitSettings.LoginPermitLimit)), "0" }
        };

        [Theory]
        [MemberData(nameof(BrokenSettings))]
        public void TheApp_RefusesToStart_WithABrokenSetting(string key, string value)
        {
            using var app = factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

            var exception = Record.Exception(() => app.CreateClient());

            Assert.Contains(Flatten(exception), inner => inner is OptionsValidationException);
        }

        // Several broken settings are reported together, in one exception
        // holding each.
        private static IEnumerable<Exception> Flatten(Exception? exception) =>
            exception switch
            {
                null => [],
                AggregateException aggregate => aggregate.InnerExceptions.SelectMany(Flatten),
                _ => Flatten(exception.InnerException).Prepend(exception)
            };
    }
}
