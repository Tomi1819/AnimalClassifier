namespace AnimalClassifier.Core.Identity.Authentication
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// The check signing in with a password makes. Identity's sign-in manager
    /// makes it, and that belongs to the web host, so Core asks for it here.
    /// </summary>
    public interface IPasswordSignInChecker
    {
        /// <summary>
        /// Unlike checking the password alone, this refuses an account that is
        /// locked out, and counts a wrong password towards locking it.
        /// </summary>
        Task<PasswordSignInResult> CheckAsync(ApplicationUser user, string password);
    }
}
