namespace AnimalClassifier.Core.Identity.Passwords
{
    using Microsoft.Extensions.Options;
    using System.Threading.RateLimiting;

    public sealed class PasswordConfirmationLimiter : IPasswordConfirmationLimiter, IDisposable
    {
        private readonly PartitionedRateLimiter<string> limiter;

        public PasswordConfirmationLimiter(IOptions<PasswordConfirmationSettings> settingsOptions)
        {
            var settings = settingsOptions.Value;

            // One window per account, so that neither the people sharing an
            // address nor the sessions sharing an account matter to the count.
            limiter = PartitionedRateLimiter.Create<string, string>(userId =>
                RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PasswordConfirmationPermitLimit,
                    Window = TimeSpan.FromMinutes(settings.PasswordConfirmationWindowMinutes)
                }));
        }

        public bool TryAcquire(string userId)
        {
            using var lease = limiter.AttemptAcquire(userId);

            return lease.IsAcquired;
        }

        public void Dispose() => limiter.Dispose();
    }
}
