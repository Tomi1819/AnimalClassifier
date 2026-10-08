namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Common.Email;
    using AnimalClassifier.Core.Common.Settings;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Infrastructure.Email;

    /// <summary>
    /// What more than one area uses: sending email, storing uploaded files, and
    /// where the frontend is served from.
    /// </summary>
    public static class CommonServiceCollectionExtension
    {
        private const string MissingEmailSettings =
            "Email:Host and Email:SenderEmail have to be set outside development.";

        private const string UploadsInsideTheApp =
            "FileUploadSettings:UploadPath has to be a folder outside the app's own outside development.";

        /// <summary>
        /// Registers the SMTP sender wherever a server is configured for it,
        /// which is how development points at a local one. Development alone
        /// may leave it unconfigured, and then logs the messages instead, so
        /// that a fresh clone runs without credentials of any kind.
        /// </summary>
        public static IServiceCollection AddApplicationEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddSettings<EmailSettings>();

            // Which sender to register depends on the settings, so they are
            // read here rather than once the app starts.
            var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>();

            if (!string.IsNullOrWhiteSpace(emailSettings?.Host)
                && !string.IsNullOrWhiteSpace(emailSettings.SenderEmail))
            {
                services.AddScoped<IEmailSender, SmtpEmailSender>();

                return services;
            }

            // Anywhere else, mail would fail one password reset at a time and
            // long after deployment. Refusing to start says so immediately.
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(MissingEmailSettings);
            }

            services.AddScoped<IEmailSender, LoggingEmailSender>();

            return services;
        }

        public static IServiceCollection AddApplicationStorage(this IServiceCollection services, IHostEnvironment environment)
        {
            // Uploads are configured relative to the content root. An empty
            // path is left for the check to refuse, where resolving it would
            // turn it into the content root itself, and mix the uploads in
            // with the app. Outside development they have to be kept apart
            // from it altogether, as a new deployment may replace it whole.
            services.AddSettings<UploadSettings>()
                .PostConfigure(settings =>
                {
                    if (!string.IsNullOrWhiteSpace(settings.UploadPath))
                    {
                        settings.UploadPath = Path.GetFullPath(settings.UploadPath, environment.ContentRootPath);
                    }
                })
                .Validate(settings => environment.IsDevelopment()
                    || string.IsNullOrWhiteSpace(settings.UploadPath)
                    || environment.IsOutsideContentRoot(settings.UploadPath),
                    UploadsInsideTheApp);

            services.AddScoped<IFileStorageService, FileStorageService>();

            return services;
        }

        public static IServiceCollection AddApplicationFrontend(this IServiceCollection services)
        {
            services.AddSettings<FrontendSettings>();

            return services;
        }
    }
}
