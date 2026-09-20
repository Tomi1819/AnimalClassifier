namespace AnimalClassifier.Core.Configurations
{
    public class RateLimitSettings
    {
        /// <summary>
        /// How many password reset requests one caller may make per window.
        /// The endpoints send mail to an address their caller chose, so
        /// without a ceiling they are a way to fill somebody's inbox, and to
        /// spend the sending quota they go through.
        ///
        /// Asking for a link and then using it share this allowance, so it has
        /// to cover a user who requests a second link, misses it, requests a
        /// third, and then needs a few attempts at a password the rules will
        /// accept. Being refused at that last step is the one place where this
        /// limit costs an honest user their reset.
        /// </summary>
        public int PasswordResetPermitLimit { get; set; } = 10;

        public int PasswordResetWindowMinutes { get; set; } = 15;
    }
}
