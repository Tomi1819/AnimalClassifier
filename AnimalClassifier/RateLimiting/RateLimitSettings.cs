namespace AnimalClassifier.RateLimiting
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The limits on the API's endpoints, each enforced by the policy of the
    /// same name in <see cref="RateLimitPolicies"/>. How often a password may
    /// be confirmed is in the same section, and read by the service that
    /// confirms it.
    /// </summary>
    public class RateLimitSettings : ISettings
    {
        public static string SectionName => "RateLimiting";

        /// <summary>
        /// How many sign-in attempts one address may make per window, right
        /// or wrong. Wrong passwords already lock the account they are tried
        /// on; this is what slows one address trying a few on every account,
        /// or locking the same account again and again.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int LoginPermitLimit { get; set; } = 10;

        [Range(1, int.MaxValue)]
        public int LoginWindowMinutes { get; set; } = 15;

        /// <summary>
        /// How many accounts one address may register per window. Each is
        /// mailed a link at an address the caller picks, so this is what keeps
        /// registering from being a way to send mail to anyone.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int RegisterPermitLimit { get; set; } = 10;

        [Range(1, int.MaxValue)]
        public int RegisterWindowMinutes { get; set; } = 60;

        /// <summary>
        /// How many password reset requests one caller may make per window.
        /// Asking for a link and using it share the allowance, so it has to
        /// cover a user who needs a second link and then a few attempts at a
        /// password the rules accept.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PasswordResetPermitLimit { get; set; } = 10;

        [Range(1, int.MaxValue)]
        public int PasswordResetWindowMinutes { get; set; } = 15;

        /// <summary>
        /// How many copies of their data one account may download per window.
        /// Each reads every file the account uploaded, and nobody needs more
        /// than one at a time; the rest is room for a download that failed.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int DataExportPermitLimit { get; set; } = 3;

        [Range(1, int.MaxValue)]
        public int DataExportWindowMinutes { get; set; } = 15;

        /// <summary>
        /// How many links to confirm its email one account may ask for per
        /// window. The address may not be the caller's own until it is confirmed.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ConfirmationEmailPermitLimit { get; set; } = 3;

        [Range(1, int.MaxValue)]
        public int ConfirmationEmailWindowMinutes { get; set; } = 15;
    }
}
