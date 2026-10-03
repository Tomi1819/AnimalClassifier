namespace AnimalClassifier.Core.Identity.Passwords
{
    /// <summary>
    /// What setting, confirming and resetting a password tell the user. Each
    /// is written to be shown as it stands.
    /// </summary>
    public static class PasswordMessages
    {
        public const string PasswordIsEmail = "The password cannot be your email address.";
        public const string IncorrectCurrentPassword = "The current password is incorrect.";
        public const string TooManyPasswordAttempts = "Too many password attempts. Please wait a few minutes and try again.";
        public const string InvalidPasswordResetLink = "This link is no longer valid. Please ask for a new one.";

        /// <summary>
        /// The answer to asking for a reset link, the same whether or not the
        /// address has an account, so that nobody can use it to learn who is
        /// registered.
        /// </summary>
        public const string PasswordResetEmailSent = "If that address has an account, a reset link is on its way.";

        public const string PasswordReset = "Your password has been changed. Please sign in.";
    }
}
