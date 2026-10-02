namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Infrastructure.Data.Models;

    public interface IPasswordConfirmer
    {
        /// <summary>
        /// Checks the password of a user who is already signed in, before a
        /// change that a session alone should not be enough for. A wrong one
        /// counts towards locking the account, the same as a failed sign-in,
        /// so a session left open cannot be used to guess it.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When the password is wrong or the account is locked.
        /// </exception>
        Task ConfirmAsync(ApplicationUser user, string password);
    }
}
