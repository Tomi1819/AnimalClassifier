namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;

    public interface IAccessTokenIssuer
    {
        /// <summary>
        /// Mints the token that stands for a signed-in session. Whether the
        /// user proved who they are with a password or a passkey is settled by
        /// the time this is called, so every way in produces the same session.
        /// </summary>
        Task<LoginResponse> IssueAsync(ApplicationUser user);

        /// <summary>
        /// Mints a token to take the place of one the caller already holds,
        /// for a change that ends it without the user proving who they are
        /// again. It runs out when the old one would have. A full lifetime
        /// would let whoever holds a token keep it alive for good, by trading
        /// it in before it expires.
        /// </summary>
        /// <param name="expiration">When the token being replaced runs out.</param>
        Task<LoginResponse> ReissueAsync(ApplicationUser user, DateTime expiration);
    }
}
