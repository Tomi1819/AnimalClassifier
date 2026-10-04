namespace AnimalClassifier.Core.Identity.Account
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.Passwords;

    /// <summary>
    /// Changes a signed-in user makes to their own account. Deleting it is
    /// <see cref="IAccountDeletionService"/>'s, being the one change that
    /// reaches past the account into everything it holds.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// The account's name, email, whether the email is confirmed, and its
        /// registration date. The token carries
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
        /// <exception cref="RequestRefusedException">
        /// When the name is blank or too long.
        /// </exception>
        Task<AccountProfile> ChangeNameAsync(string userId, ChangeNameRequest request);

        /// <summary>
        /// Sets a new password once the current one has been confirmed. The
        /// account's password may be checked only so often, so a session left
        /// open cannot be used to guess it; see <see cref="IPasswordConfirmer"/>.
        ///
        /// Succeeding changes the account's security stamp, which ends every
        /// other session, so the caller's own session is issued afresh.
        /// </summary>
        /// <returns>The token the caller's session continues with.</returns>
        /// <exception cref="RequestRefusedException">
        /// When the current password is wrong or has been checked too often, or
        /// the new password fails the rules or is the one the account already has.
        /// </exception>
        Task<LoginResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request);

        /// <summary>
        /// Ends every session the account has by changing its security stamp,
        /// which every token is checked against. That includes tokens nobody
        /// can sign out any other way, such as one copied off a device. The
        /// caller's own session is issued afresh, so only the others end.
        ///
        /// No password is asked for, so the new token runs out when the
        /// caller's old one would have, rather than starting a full lifetime.
        /// </summary>
        /// <param name="sessionExpiration">When the caller's token runs out.</param>
        /// <returns>The token the caller's session continues with.</returns>
        Task<LoginResponse> SignOutOtherSessionsAsync(string userId, DateTime sessionExpiration);
    }
}
