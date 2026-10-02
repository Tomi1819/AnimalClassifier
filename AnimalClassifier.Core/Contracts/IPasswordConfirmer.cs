namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.Exceptions;
    using AnimalClassifier.Infrastructure.Data.Models;

    public interface IPasswordConfirmer
    {
        /// <summary>
        /// Checks the password of a user who is already signed in, before a
        /// change that a session alone should not be enough for.
        ///
        /// Each account may be checked only so often, whichever change asks,
        /// so a session left open cannot be used to guess the password. The
        /// lockout that guards signing in plays no part: it neither stops a
        /// check nor is added to by one. Anyone can lock an account out from
        /// outside, and that must not keep its owner from changing their
        /// password; and a wrong guess made in here must not keep them from
        /// signing in.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the password is wrong, or has been checked too often.
        /// </exception>
        Task ConfirmAsync(ApplicationUser user, string password);
    }
}
