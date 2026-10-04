namespace AnimalClassifier.Tests.Support
{
    using AnimalClassifier.Core.Common.Email;
    using AnimalClassifier.Core.Common.Settings;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.RateLimiting;
    using AnimalClassifier.Tests.Identity;
    using AnimalClassifier.Tests.Recognitions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Runs the API against its own LocalDB database, created from the
    /// migrations and dropped once the tests finish.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>
    {
        /// <summary>
        /// The domain passkeys are expected to bind to, which is the
        /// frontend's rather than the API's.
        /// </summary>
        public const string FrontendDomain = "frontend.test";

        public RecordingEmailSender Emails { get; } = new();

        /// <summary>
        /// What every upload is classified by, in place of the model.
        /// </summary>
        public StubImageClassifier Classifier { get; } = new();

        /// <summary>
        /// What every uploaded video's frames are read by, in place of OpenCV.
        /// </summary>
        public StubVideoFrameSampler FrameSampler { get; } = new();

        private readonly string connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=AnimalClassifierTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;";

        private readonly string uploadPath =
            Path.Combine(Path.GetTempPath(), $"AnimalClassifierTests_{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Outside Development nothing else supplies these settings, so the
            // tests can never reach the development database.
            builder.UseEnvironment("Testing");
            builder.UseSetting($"ConnectionStrings:{AnimalClassifierDbContext.ConnectionStringName}", connectionString);

            // Kept apart from the app's own uploads, and removed with the database.
            builder.UseSetting(Key<UploadSettings>(nameof(UploadSettings.UploadPath)), uploadPath);
            builder.UseSetting(Key<JwtSettings>(nameof(JwtSettings.SecretKey)), "TEST-ONLY-SIGNING-KEY-NOT-FOR-PRODUCTION-USE");

            // The app refuses to start outside Development without these, and
            // nothing here ever connects to the host they name.
            builder.UseSetting(Key<EmailSettings>(nameof(EmailSettings.Host)), "localhost");
            builder.UseSetting(Key<EmailSettings>(nameof(EmailSettings.SenderEmail)), "tests@animalclassifier.local");
            builder.UseSetting(Key<FrontendSettings>(nameof(FrontendSettings.BaseUrl)), $"https://{FrontendDomain}");

            // Requests from a test carry no client address, so all of them
            // share one rate limiting window and the deployed limit would
            // throttle the suite. The tests that cover the limits set their own.
            builder.UseSetting(Key<RateLimitSettings>(nameof(RateLimitSettings.LoginPermitLimit)), "1000");
            builder.UseSetting(Key<RateLimitSettings>(nameof(RateLimitSettings.RegisterPermitLimit)), "1000");
            builder.UseSetting(Key<RateLimitSettings>(nameof(RateLimitSettings.PasswordResetPermitLimit)), "1000");

            // Whatever the app sends is kept here rather than sent, which is
            // also what stops a test run from mailing anyone. Uploads are
            // recognised by stand-ins, which a test can tell what to see.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);

                services.RemoveAll<IImageClassifier>();
                services.AddSingleton<IImageClassifier>(Classifier);

                services.RemoveAll<IVideoFrameSampler>();
                services.AddSingleton<IVideoFrameSampler>(FrameSampler);
            });
        }

        /// <summary>
        /// The configuration key of one of the settings, such as
        /// <c>Jwt:SecretKey</c>.
        /// </summary>
        public static string Key<TSettings>(string setting) where TSettings : ISettings =>
            $"{TSettings.SectionName}:{setting}";

        /// <summary>
        /// The same app with an authenticator standing in for the real one, so
        /// that a test can get past the cryptography to what surrounds it.
        /// </summary>
        public WebApplicationFactory<Program> WithPasskeyHandler(StubPasskeyHandler handler) =>
            WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPasskeyHandler<ApplicationUser>>();
                services.AddSingleton<IPasskeyHandler<ApplicationUser>>(handler);
            }));

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // The app seeds roles on startup, so the schema has to exist first.
            using var context = CreateContext();
            context.Database.Migrate();

            return base.CreateHost(builder);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();

            if (Directory.Exists(uploadPath))
            {
                Directory.Delete(uploadPath, recursive: true);
            }
        }

        // Built without the app's Identity options, so its model differs from
        // the migrations, which are what create the schema.
        private AnimalClassifierDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<AnimalClassifierDbContext>()
                .UseSqlServer(connectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
    }
}
