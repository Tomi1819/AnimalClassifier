namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Configurations;
    using System.Threading.RateLimiting;
    using static Constants.MessageConstants;
    using static Core.Constants.ConfigConstants;

    public static class RateLimitingServiceCollectionExtension
    {
        private const string UnknownClient = "unknown";

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
    }
}
