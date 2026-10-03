namespace AnimalClassifier.Core.Identity.Authentication
{
    /// <summary>
    /// What registering and signing in tell the user. Each is written to be
    /// shown as it stands.
    /// </summary>
    public static class AuthenticationMessages
    {
        public const string AlreadyRegisteredEmail = "This email is already registered.";
        public const string InvalidCredentials = "Invalid email or password.";

        /// <summary>
        /// Given whichever way the user tried to sign in, a password or a
        /// passkey.
        /// </summary>
        public const string LockedOutAccount = "This account is locked.";
    }
}
