namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Services;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.ML;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.ML;
    using System;
    using System.Text;
    using System.Threading.RateLimiting;
    using static Core.Constants.ConfigConstants;
    using static Core.Constants.SecurityConstants;
    using static Constants.MessageConstants;
    using AnimalClassifier.Core.Services.Helpers;

    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Stands in for the address of a caller the server cannot see one for,
        /// who then shares a window with every other such caller.
        /// </summary>
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
            services.AddScoped<IRepository, Repository>();
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRecognitionService, RecognitionService>();
            services.AddScoped<IFileValidator, FileValidator>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IAnimalService, AnimalService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
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
            services.Configure<JwtSettings>(configuration.GetSection(Jwt));
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
        /// Registers whichever email sender suits the environment: development
        /// logs the messages instead of sending them, while everywhere else
        /// mail goes out over SMTP and the settings for it have to be there.
        /// </summary>
        public static IServiceCollection AddApplicationEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.Configure<EmailSettings>(configuration.GetSection(Email));

            if (environment.IsDevelopment())
            {
                services.AddScoped<IEmailSender, LoggingEmailSender>();

                return services;
            }

            var emailSettings = configuration.GetSection(Email).Get<EmailSettings>();

            // Without these two, mail fails one password reset at a time, long
            // after deployment. Refusing to start says so immediately instead.
            if (string.IsNullOrWhiteSpace(emailSettings?.Host)
                || string.IsNullOrWhiteSpace(emailSettings.SenderEmail))
            {
                throw new InvalidOperationException(MissingEmailSettings);
            }

            services.AddScoped<IEmailSender, SmtpEmailSender>();

            return services;
        }

        /// <summary>
        /// Caps how often one caller may ask for a password reset. The two
        /// endpoints send mail to an address the caller picks and hand out
        /// attempts at a token, neither of which should be available without
        /// limit.
        /// </summary>
        public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var rateLimitSettings = configuration.GetSection(RateLimiting).Get<RateLimitSettings>()
                ?? new RateLimitSettings();

            services.AddRateLimiter(options =>
            {
                options.AddPolicy<string>(PasswordResetPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        // Callers sharing an address share a window. Counting
                        // them all as one instead would let a single caller
                        // spend everybody's attempts.
                        context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitSettings.PasswordResetPermitLimit,
                            Window = TimeSpan.FromMinutes(rateLimitSettings.PasswordResetWindowMinutes)
                        }));

                // Otherwise the refusal arrives as a bare status the frontend
                // has nothing to show for.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new { message = TooManyRequests }, cancellationToken);
                };
            });

            return services;
        }

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

        public static IServiceCollection AddApplicationIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Stores.MaxLengthForKeys = 128;
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AnimalClassifierDbContext>()
                .AddSignInManager()
                // Nothing generates the one-time tokens a password reset needs
                // until these are registered.
                .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
                options.TokenLifespan = PasswordResetTokenLifespan);

            var jwtSettings = configuration.GetSection(Jwt).Get<JwtSettings>();

            if (string.IsNullOrEmpty(jwtSettings?.SecretKey))
            {
                throw new InvalidOperationException(MissingJwtSecurityKey);
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                    };

                    // A signed token would otherwise stay valid until it expires, so a
                    // change to the user's access takes effect on their next request.
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

                            if (!await authService.IsSessionValidAsync(context.Principal!))
                            {
                                context.Fail(OutdatedToken);
                            }
                        }
                    };
                });

            return services;
        }
    }
}
