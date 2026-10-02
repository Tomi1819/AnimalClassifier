namespace AnimalClassifier.Core.Contracts
{
    /// <summary>
    /// Caps how often one account's password may be checked from inside a
    /// session, which is what stops a session left open from being used to
    /// guess it.
    ///
    /// It is kept apart from the lockout that guards signing in. Anyone can
    /// cause that one from outside, by getting the password wrong, and it
    /// must not come between a signed-in owner and their own account.
    /// </summary>
    public interface IPasswordConfirmationLimiter
    {
        /// <summary>
        /// Counts one check against the account's allowance.
        /// </summary>
        /// <returns>Whether the account still had a check left.</returns>
        bool TryAcquire(string userId);
    }
}
