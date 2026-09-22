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
    }
}
