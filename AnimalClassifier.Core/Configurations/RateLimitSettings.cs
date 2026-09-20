namespace AnimalClassifier.Core.Configurations
{
    public class RateLimitSettings
    {
        /// <summary>
        /// How many password reset requests one caller may make per window.
        /// The endpoint sends mail to an address its caller chose, so without
        /// a ceiling it is a way to fill somebody's inbox, and to spend the
        /// sending quota it goes through.
        /// </summary>
        public int PasswordResetPermitLimit { get; set; } = 5;

        public int PasswordResetWindowMinutes { get; set; } = 15;
    }
}
