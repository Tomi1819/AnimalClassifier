namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Common.Settings;
    using AnimalClassifier.Core.Identity.Account;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Identity.Passkeys;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Core.Identity.SecurityAlerts;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

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

        private const string OutdatedToken = "The token no longer matches the account.";

        private const string UnresolvedPasskeyServerDomain =
            "Passkey:ServerDomain is not set, and no domain can be read from Frontend:BaseUrl.";

        /// <summary>
        /// The accounts Identity keeps, the rules their passwords are held to,
        /// and the services that sign them in and change them.
        /// </summary>
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
                    options.Password.RequiredLength = PasswordPolicy.MinLength;
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
                options.TokenLifespan = PasswordPolicy.ResetTokenLifespan);

            return services.AddIdentityServices();
        }

        /// <summary>
        /// Has every request show who it comes from with a token this app
        /// signed, and refuses one that no longer speaks for its account.
        /// </summary>
        public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services)
        {
            services.AddSettings<JwtSettings>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            // Read from the settings once they can be, which is after they
            // have been checked.
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
                {
                    var jwtSettings = jwtOptions.Value;

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
        public static IServiceCollection AddApplicationPasskeys(this IServiceCollection services)
        {
            services.AddSettings<PasskeySettings>()
                .Validate<IOptions<FrontendSettings>>(
                    (passkeySettings, frontendOptions) => passkeySettings.ResolveServerDomain(frontendOptions.Value) is not null,
                    UnresolvedPasskeyServerDomain);

            services.AddOptions<IdentityPasskeyOptions>()
                .Configure<IOptions<PasskeySettings>, IOptions<FrontendSettings>>((options, passkeyOptions, frontendOptions) =>
                {
                    // Left to the host header, this would be whatever a caller put
                    // there, and a passkey could be bound to a domain of their
                    // choosing.
                    options.ServerDomain = passkeyOptions.Value.ResolveServerDomain(frontendOptions.Value);

                    // Signing in without first naming an account needs the
                    // credential to be discoverable, which is what the browser
                    // offers an account picker from.
                    options.ResidentKeyRequirement = RequiredResidentKey;
                });

            return services;
        }

        // Grouped as the folders under Core/Identity are, so that a part of
        // identity and what it registers are found the same way.
        private static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAccessTokenIssuer, AccessTokenIssuer>();

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IDataExportService, DataExportService>();
            services.AddScoped<IAccountDeletionService, AccountDeletionService>();

            services.AddScoped<IPasswordConfirmer, PasswordConfirmer>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();

            // Kept for as long as the app runs, since it is what remembers
            // the checks already made.
            services.AddSettings<PasswordConfirmationSettings>();
            services.AddSingleton<IPasswordConfirmationLimiter, PasswordConfirmationLimiter>();

            services.AddScoped<IPasskeyService, PasskeyService>();
            services.AddSingleton<IPasskeyStateProtector, PasskeyStateProtector>();

            services.AddScoped<ISecurityAlertSender, SecurityAlertSender>();

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
    }
}
