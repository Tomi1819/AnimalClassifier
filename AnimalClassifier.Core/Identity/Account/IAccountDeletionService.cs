namespace AnimalClassifier.Core.Identity.Account
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.Passwords;

    /// <summary>
    /// A signed-in user deleting their own account, and everything it holds.
    /// </summary>
    public interface IAccountDeletionService
    {
        /// <summary>
        /// Deletes the account once its password has been confirmed, along with
        /// its recognitions, uploads and passkeys. The admin audit log keeps its
        /// entries, naming a deleted user in the account's place. Every session
        /// ends with it, since a token is only good for an account that exists.
        /// The password may be checked only so often; see
        /// <see cref="IPasswordConfirmer"/>.
        ///
        /// An administrator is refused, so that there is always someone left to
        /// manage the site; another administrator has to revoke the role first.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the password is wrong or has been checked too often, or the
        /// account belongs to an administrator.
        /// </exception>
        Task DeleteAccountAsync(string userId, DeleteAccountRequest request);
    }
}
