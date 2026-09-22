namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
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
        Task<PasskeyOptionsResponse> CreateRegistrationOptionsAsync(string userId, HttpContext httpContext);

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
