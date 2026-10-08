namespace AnimalClassifier.Core.Identity.EmailConfirmation
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity.EmailConfirmation.Models;

    /// <summary>
    /// Showing that an account's email belongs to whoever registered it, by a
    /// link mailed to it. Signing in does not wait for it. What waits is
    /// anything that trusts the address, such as Admin:Email, which would
    /// otherwise make an administrator of whoever registered it first.
    /// </summary>
    public interface IEmailConfirmationService
    {
        /// <summary>
        /// Emails a newly registered account a link to confirm its address. A
        /// mail server that fails is logged rather than reported, since the
        /// account exists either way and another link can be asked for.
        /// </summary>
        Task SendLinkAsync(ApplicationUser user);

        /// <summary>
        /// Emails a signed-in user another link, for one whose first went
        /// astray or ran out.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the email is confirmed already.
        /// </exception>
        Task ResendLinkAsync(string userId);

        /// <summary>
        /// Confirms the email, if the token was issued for that account and
        /// has not expired. Sessions carry on.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the link cannot be used.
        /// </exception>
        Task ConfirmAsync(ConfirmEmailRequest request);
    }
}
