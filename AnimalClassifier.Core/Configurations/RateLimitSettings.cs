namespace AnimalClassifier.Core.Configurations
{
    public class RateLimitSettings
    {
        /// <summary>
        /// How many password reset requests one caller may make per window.
        /// Asking for a link and using it share the allowance, so it has to
        /// cover a user who needs a second link and then a few attempts at a
        /// password the rules accept.
        /// </summary>
        public int PasswordResetPermitLimit { get; set; } = 10;

        public int PasswordResetWindowMinutes { get; set; } = 15;
    }
}
