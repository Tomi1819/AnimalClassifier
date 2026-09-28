namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;

    /// <summary>
    /// Changes a signed-in user makes to their own account.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// The account's name, email and registration date. The token carries
        /// only the email, so this is where a page reads the rest.
        /// </summary>
        Task<AccountProfile> GetProfileAsync(string userId);

        /// <summary>
        /// Sets the name the account goes by. It is kept as typed apart from
        /// its spacing, so that a name such as "McDonald" can be put right.
        /// Sessions carry on, since the name is not part of signing in.
        ///
        /// Passkeys already registered keep the name they were created with,
        /// as that copy lives on the user's device.
        /// </summary>
        /// <returns>The profile with the new name.</returns>
        /// <exception cref="InvalidOperationException">
        /// When the name is blank or too long.
        /// </exception>
        Task<AccountProfile> ChangeNameAsync(string userId, ChangeNameRequest request);

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

        /// <summary>
        /// Ends every session the account has by changing its security stamp,
        /// which every token is checked against. That includes tokens nobody
        /// can sign out any other way, such as one copied off a device. The
        /// caller's own session is issued afresh, so only the others end.
        /// </summary>
        /// <returns>The token the caller's session continues with.</returns>
        Task<LoginResponse> SignOutOtherSessionsAsync(string userId);

        /// <summary>
        /// Deletes the account once its password has been confirmed, along with
        /// its recognitions, uploads and passkeys. The admin audit log keeps its
        /// entries, naming a deleted user in the account's place. Every session
        /// ends with it, since a token is only good for an account that exists.
        ///
        /// An administrator is refused, so that there is always someone left to
        /// manage the site; another administrator has to revoke the role first.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When the password is wrong, the account is locked, or it belongs to
        /// an administrator.
        /// </exception>
        Task DeleteAccountAsync(string userId, DeleteAccountRequest request);
    }
}
