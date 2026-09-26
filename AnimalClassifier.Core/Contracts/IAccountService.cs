namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;

    /// <summary>
    /// Changes a signed-in user makes to their own account.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Sets a new password once the current one has been confirmed. A wrong
        /// current password counts towards locking the account, the same as a
        /// failed sign-in, so a session left open cannot be used to guess it.
        ///
        /// Succeeding changes the account's security stamp, which ends every
        /// other session, so the caller's own session is issued afresh.
        /// </summary>
        /// <returns>The token the caller's session continues with.</returns>
        /// <exception cref="InvalidOperationException">
        /// When the current password is wrong, the account is locked, or the new
        /// password fails the rules.
        /// </exception>
        Task<LoginResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    }
}
