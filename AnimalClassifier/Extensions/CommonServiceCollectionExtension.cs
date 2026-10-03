namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Common.Email;
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.Services;
    using static Constants.MessageConstants;
    using static Core.Constants.ConfigConstants;

    /// <summary>
    /// What more than one area uses: sending email, storing uploaded files, and
    /// where the frontend is served from.
    /// </summary>
    public static class CommonServiceCollectionExtension
    {
        /// <summary>
        /// Registers the SMTP sender wherever a server is configured for it,
        /// which is how development points at a local one. Development alone
        /// may leave it unconfigured, and then logs the messages instead, so
        /// that a fresh clone runs without credentials of any kind.
        /// </summary>
        public static IServiceCollection AddApplicationEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.Configure<EmailSettings>(configuration.GetSection(Email));

            var emailSettings = configuration.GetSection(Email).Get<EmailSettings>();

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

        public static IServiceCollection AddApplicationStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var uploadSettings = configuration.GetSection(FileUploadSettings).Get<UploadSettings>();
            if (string.IsNullOrWhiteSpace(uploadSettings?.UploadPath))
            {
                throw new InvalidOperationException(MissingUploadPath);
            }

            services.Configure<UploadSettings>(configuration.GetSection(FileUploadSettings));

            // Uploads are configured relative to the content root.
            services.PostConfigure<UploadSettings>(settings =>
                settings.UploadPath = Path.GetFullPath(settings.UploadPath, environment.ContentRootPath));

            services.AddScoped<IFileStorageService, FileStorageService>();

            return services;
        }

        public static IServiceCollection AddApplicationFrontend(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FrontendSettings>(configuration.GetSection(Frontend));

            // Emailed links are built from this, and a link to nowhere is only
            // discovered by the user who cannot get back into their account.
            var frontendSettings = configuration.GetSection(Frontend).Get<FrontendSettings>();
            if (string.IsNullOrWhiteSpace(frontendSettings?.BaseUrl))
            {
                throw new InvalidOperationException(MissingFrontendBaseUrl);
            }

            return services;
        }
    }
}
