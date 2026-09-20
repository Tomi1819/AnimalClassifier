namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Infrastructure.Data;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using static AnimalClassifier.Core.Constants.ConfigConstants;

    /// <summary>
    /// Runs the API against its own LocalDB database, created from the
    /// migrations and dropped once the tests finish.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>
    {
        /// <summary>
        /// Every message the app tried to send during the test run.
        /// </summary>
        public RecordingEmailSender Emails { get; } = new();

        private readonly string connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=AnimalClassifierTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Outside Development nothing else supplies these settings, so the
            // tests can never reach the development database.
            builder.UseEnvironment("Testing");
            builder.UseSetting($"ConnectionStrings:{DefaultConnection}", connectionString);
            builder.UseSetting($"{Jwt}:SecretKey", "TEST-ONLY-SIGNING-KEY-NOT-FOR-PRODUCTION-USE");

            // The app refuses to start outside Development without these, and
            // nothing here ever connects to the host they name.
            builder.UseSetting($"{Email}:Host", "localhost");
            builder.UseSetting($"{Email}:SenderEmail", "tests@animalclassifier.local");
            builder.UseSetting($"{Frontend}:BaseUrl", "https://frontend.test");

            // Requests from a test carry no client address, so all of them
            // share one rate limiting window and the deployed limit would
            // throttle the suite. The test that covers the limit sets its own.
            builder.UseSetting($"{RateLimiting}:PasswordResetPermitLimit", "1000");

            // Whatever the app sends is kept here rather than sent, which is
            // also what stops a test run from mailing anyone.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
            });
        }

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
