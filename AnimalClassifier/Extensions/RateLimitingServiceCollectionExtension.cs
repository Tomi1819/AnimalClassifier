namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.ErrorHandling;
    using AnimalClassifier.RateLimiting;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Options;
    using System.Threading.RateLimiting;

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
        public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
        {
            services.AddSettings<RateLimitSettings>();

            // Otherwise the refusal arrives as a bare status the frontend
            // has nothing to show for.
            services.AddRateLimiter(options => options.OnRejected = RespondTooManyRequestsAsync);

            // Read from the settings once they can be, which is after they
            // have been checked.
            services.AddOptions<RateLimiterOptions>()
                .Configure<IOptions<RateLimitSettings>>((options, settings) => AddPolicies(options, settings.Value));

            return services;
        }

        private static void AddPolicies(RateLimiterOptions options, RateLimitSettings settings)
        {
            options.AddPolicy<string>(RateLimitPolicies.Login, context => LimitPerAddress(
                context, settings.LoginPermitLimit, settings.LoginWindowMinutes));

            options.AddPolicy<string>(RateLimitPolicies.PasswordReset, context => LimitPerAddress(
                context, settings.PasswordResetPermitLimit, settings.PasswordResetWindowMinutes));

            // Counted per account rather than per address, as only a
            // signed-in user can export, and people sharing an address
            // should not use up each other's exports.
            options.AddPolicy<string>(RateLimitPolicies.DataExport, context => FixedWindow(
                context.User.Id() ?? UnknownClient, settings.DataExportPermitLimit, settings.DataExportWindowMinutes));
        }

        // Callers sharing an address share a window. Counting them all as one
        // instead would let a single caller spend everybody's attempts.
        private static RateLimitPartition<string> LimitPerAddress(HttpContext context, int permitLimit, int windowMinutes) =>
            FixedWindow(context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient, permitLimit, windowMinutes);

        private static RateLimitPartition<string> FixedWindow(string partitionKey, int permitLimit, int windowMinutes) =>
            RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes)
            });

        private static async ValueTask RespondTooManyRequestsAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await context.HttpContext.Response.WriteAsJsonAsync(
                new MessageResponse { Message = ErrorMessages.TooManyRequests }, cancellationToken);
        }
    }
}
