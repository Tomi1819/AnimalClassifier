namespace AnimalClassifier.Core.Identity.Passwords
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The limit <see cref="PasswordConfirmationLimiter"/> enforces. It sits
    /// in the same section as the limits on the API's endpoints, so that every
    /// limit is configured in one place.
    /// </summary>
    public class PasswordConfirmationSettings : ISettings
    {
        public static string SectionName => "RateLimiting";

        /// <summary>
        /// How many times one account's password may be checked from inside
        /// a session per window, right or wrong: before a password change, a
        /// new passkey, or the account being deleted. It has to leave room
        /// for a few typos and a passkey prompt cancelled a few times, and
        /// still be far too few to guess with.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PasswordConfirmationPermitLimit { get; set; } = 10;

        [Range(1, int.MaxValue)]
        public int PasswordConfirmationWindowMinutes { get; set; } = 15;
    }
}
