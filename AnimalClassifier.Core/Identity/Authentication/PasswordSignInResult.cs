namespace AnimalClassifier.Core.Identity.Authentication
{
    /// <summary>
    /// What checking a password at sign-in found.
    /// </summary>
    public enum PasswordSignInResult
    {
        Succeeded,

        /// <summary>
        /// The password was wrong, or the account may not sign in.
        /// </summary>
        Failed,

        /// <summary>
        /// The account is locked, whatever the password.
        /// </summary>
        LockedOut
    }
}
