namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Common.Email;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Services;
    using AnimalClassifier.Core.Services.Helpers;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.ML;
    using Microsoft.ML;
    using System;
    using System.Threading.RateLimiting;
    using static Core.Constants.ConfigConstants;
    using static Constants.MessageConstants;

    public static class ServiceCollectionExtension
    {
        private const string UnknownClient = "unknown";

        public static IServiceCollection AddApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(DefaultConnection)
                ?? throw new InvalidOperationException(MissingConnectionString);

            services.AddDbContext<AnimalClassifierDbContext>(options =>
                options.UseSqlServer(connectionString));

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddScoped<IRecognitionLogRepository, RecognitionLogRepository>();
            services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IRecognitionService, RecognitionService>();
            services.AddScoped<IFileValidator, FileValidator>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IAnimalService, AnimalService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddSingleton<MLContext>();

            var uploadSettings = configuration.GetSection(FileUploadSettings).Get<UploadSettings>();
            if (string.IsNullOrWhiteSpace(uploadSettings?.UploadPath))
            {
                throw new InvalidOperationException(MissingUploadPath);
            }

            services.Configure<UploadSettings>(configuration.GetSection(FileUploadSettings));

            // Uploads are configured relative to the content root.
            services.PostConfigure<UploadSettings>(settings =>
                settings.UploadPath = Path.GetFullPath(settings.UploadPath, environment.ContentRootPath));

            services.Configure<MLModelSettings>(configuration.GetSection(MLModel));
            services.Configure<FrontendSettings>(configuration.GetSection(Frontend));

            // Emailed links are built from this, and a link to nowhere is only
            // discovered by the user who cannot get back into their account.
            var frontendSettings = configuration.GetSection(Frontend).Get<FrontendSettings>();
            if (string.IsNullOrWhiteSpace(frontendSettings?.BaseUrl))
            {
                throw new InvalidOperationException(MissingFrontendBaseUrl);
            }

            var mlModelSettings = configuration.GetSection(MLModel).Get<MLModelSettings>();
            if (string.IsNullOrWhiteSpace(mlModelSettings?.Path))
            {
                throw new InvalidOperationException(MissingMLModelPath);
            }

            services.AddPredictionEnginePool<ImageData, ImagePrediction>()
                .FromFile(mlModelSettings.Path);

            return services;
        }

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

        /// <summary>
        /// Caps how often one caller may try to sign in, since a lockout
        /// guards only the account a wrong password was tried on; how often
        /// one may ask for a password reset, since the endpoints mail an
        /// address the caller picks and hand out attempts at a token; and how
        /// often one account may export its data, since each export reads
        /// every file the account uploaded.
        ///
        /// How often an account's password may be confirmed from inside a
        /// session is capped as well, by the service that confirms it rather
        /// than by an endpoint, so that nothing asking for the password can
        /// leave the cap out.
        /// </summary>
        public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var rateLimitSettings = configuration.GetSection(RateLimiting).Get<RateLimitSettings>()
                ?? new RateLimitSettings();

            services.Configure<RateLimitSettings>(configuration.GetSection(RateLimiting));

            services.AddRateLimiter(options =>
            {
                options.AddPolicy<string>(LoginPolicy, context => LimitPerAddress(
                    context, rateLimitSettings.LoginPermitLimit, rateLimitSettings.LoginWindowMinutes));

                options.AddPolicy<string>(PasswordResetPolicy, context => LimitPerAddress(
                    context, rateLimitSettings.PasswordResetPermitLimit, rateLimitSettings.PasswordResetWindowMinutes));

                // Counted per account rather than per address, as only a
                // signed-in user can export, and people sharing an address
                // should not use up each other's exports.
                options.AddPolicy<string>(DataExportPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.User.Id() ?? UnknownClient,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitSettings.DataExportPermitLimit,
                            Window = TimeSpan.FromMinutes(rateLimitSettings.DataExportWindowMinutes)
                        }));

                // Otherwise the refusal arrives as a bare status the frontend
                // has nothing to show for.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new MessageResponse { Message = TooManyRequests }, cancellationToken);
                };
            });

            return services;
        }

        // Callers sharing an address share a window. Counting them all as one
        // instead would let a single caller spend everybody's attempts.
        private static RateLimitPartition<string> LimitPerAddress(HttpContext context, int permitLimit, int windowMinutes) =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(windowMinutes)
                });

        public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection(CorsAllowedOrigins).Get<string[]>()
                ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            return services;
        }
    }
}
