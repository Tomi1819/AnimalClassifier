namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Registering and using passkeys. Each ceremony takes two calls: one for
    /// the options the browser needs, then one carrying what the authenticator
    /// made of them, along with the state the options were issued with.
    ///
    /// The <see cref="HttpContext"/> is passed through to Identity, which reads
    /// the origin of the request from it to check that the ceremony is being
    /// finished where it was started.
    /// </summary>
    public interface IPasskeyService
    {
        /// <summary>
        /// Options for registering a passkey, once the account's password has
        /// been confirmed. A passkey is a way in that outlasts the session
        /// asking for it, and a password change leaves it in place, so holding
        /// a session is not enough to add one. The password may be checked
        /// only so often; see <see cref="IPasswordConfirmer"/>.
        ///
        /// The state the options come with is what registering then requires,
        /// so the passkey itself cannot be added without getting past this.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the password is wrong or has been checked too often.
        /// </exception>
        Task<PasskeyOptionsResponse> CreateRegistrationOptionsAsync(string userId, PasskeyRegistrationOptionsRequest request, HttpContext httpContext);

        /// <summary>
        /// Registers a new passkey to the user who asked for the options.
        /// </summary>
        Task<PasskeySummary> RegisterAsync(string userId, PasskeyRegistrationRequest request, HttpContext httpContext);

        /// <summary>
        /// Options for signing in. No account is named, so the browser offers
        /// whichever passkeys it holds for this site and the user picks.
        /// </summary>
        Task<PasskeyOptionsResponse> CreateLoginOptionsAsync(HttpContext httpContext);

        Task<LoginResponse> LoginAsync(PasskeyCredentialRequest request, HttpContext httpContext);

        Task<IEnumerable<PasskeySummary>> GetPasskeysAsync(string userId);

        Task RemoveAsync(string userId, string passkeyId);
    }
}
