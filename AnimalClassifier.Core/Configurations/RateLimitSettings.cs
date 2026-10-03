namespace AnimalClassifier.Core.Configurations
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

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
    }
}
