namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.Services.Helpers;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.IdentityModel.Tokens;
    using static Constants.MessageConstants;
    using static Core.Constants.ConfigConstants;
    using static Core.Constants.SecurityConstants;
    using static Core.Constants.ValidationConstants;

    /// <summary>
    /// Sets up who the users are and how a request shows which of them it
    /// comes from: the accounts Identity keeps, the tokens that stand for a
    /// session, and the passkeys that can open one.
    /// </summary>
    public static class IdentityServiceCollectionExtension
    {
        // The length the schema's key columns were created with.
        private const int StoreKeyMaxLength = 128;

        // The WebAuthn value asking the authenticator for a discoverable credential.
        private const string RequiredResidentKey = "required";

        public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
        {
            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Stores.MaxLengthForKeys = StoreKeyMaxLength;

                    // Passkeys are stored from this version of the schema onwards;
                    // below it Identity refuses to keep them at all.
                    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
                    options.SignIn.RequireConfirmedAccount = false;

                    // Length is what makes a password hard to guess. Rules
                    // about which characters it holds mostly produce the same
                    // few patterns, so there are none.
                    options.Password.RequiredLength = PasswordMinLength;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AnimalClassifierDbContext>()
                .AddSignInManager()
                .AddPasswordValidator<EmailAsPasswordValidator>()
                // Nothing generates the one-time tokens a password reset needs
                // until these are registered.
                .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
                options.TokenLifespan = PasswordResetTokenLifespan);

            return services;
        }

        /// <summary>
        /// Has every request show who it comes from with a token this app
        /// signed, and refuses one that no longer speaks for its account.
        /// </summary>
        public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection(Jwt);
            var jwtSettings = jwtSection.Get<JwtSettings>();

            if (string.IsNullOrEmpty(jwtSettings?.SecretKey))
            {
                throw new InvalidOperationException(MissingJwtSecurityKey);
            }

            services.Configure<JwtSettings>(jwtSection);

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
                        IssuerSigningKey = jwtSettings.CreateSigningKey()
                    };

                    options.Events = new JwtBearerEvents { OnTokenValidated = RejectOutdatedTokenAsync };
                });

            return services;
        }

        /// <summary>
        /// Points passkeys at the domain the browser shows the user. That is the
        /// frontend's domain rather than this API's, because an authenticator
        /// binds a credential to the page that asked for it and will not offer it
        /// anywhere else. Splitting the two across unrelated domains leaves no
        /// domain that covers both, and passkeys cannot be used at all.
        /// </summary>
        public static IServiceCollection AddApplicationPasskeys(this IServiceCollection services, IConfiguration configuration)
        {
            // Read once and folded into IdentityPasskeyOptions below, which is
            // the form everything downstream asks for.
            var passkeySettings = configuration.GetSection(Passkey).Get<PasskeySettings>();
            var frontendSettings = configuration.GetSection(Frontend).Get<FrontendSettings>();

            var serverDomain = string.IsNullOrWhiteSpace(passkeySettings?.ServerDomain)
                ? ReadHost(frontendSettings?.BaseUrl)
                : passkeySettings.ServerDomain;

            if (string.IsNullOrWhiteSpace(serverDomain))
            {
                throw new InvalidOperationException(InvalidPasskeyServerDomain);
            }

            services.Configure<IdentityPasskeyOptions>(options =>
            {
                // Left to the host header, this would be whatever a caller put
                // there, and a passkey could be bound to a domain of their
                // choosing.
                options.ServerDomain = serverDomain;

                // Signing in without first naming an account needs the
                // credential to be discoverable, which is what the browser
                // offers an account picker from.
                options.ResidentKeyRequirement = RequiredResidentKey;
            });

            return services;
        }

        // A signed token would otherwise stay valid until it expires, so a
        // change to the user's access takes effect on their next request.
        private static async Task RejectOutdatedTokenAsync(TokenValidatedContext context)
        {
            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

            if (!await authService.IsSessionValidAsync(context.Principal!))
            {
                context.Fail(OutdatedToken);
            }
        }

        private static string? ReadHost(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var parsed) ? parsed.Host : null;
    }
}
