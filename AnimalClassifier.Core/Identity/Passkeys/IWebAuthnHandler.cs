namespace AnimalClassifier.Core.Identity.Passkeys
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity.Passkeys.Models;

    /// <summary>
    /// The WebAuthn half of a passkey ceremony: the options a browser is asked
    /// with, and the checks on what its authenticator made of them. Identity's
    /// passkey handler makes both, and reads the origin of the request to
    /// check that a ceremony is finished where it was started, so it belongs
    /// to the web host and Core asks for it here.
    ///
    /// Nothing here decides who may do what. Whose a new passkey is, and
    /// whether an account may sign in, are <see cref="IPasskeyService"/>'s to
    /// check.
    /// </summary>
    public interface IWebAuthnHandler
    {
        /// <summary>
        /// Options for registering a new passkey to the account.
        /// </summary>
        Task<PasskeyChallenge> CreateRegistrationOptionsAsync(ApplicationUser user);

        /// <returns>
        /// The new passkey and the account the state names, or null when the
        /// authenticator's answer does not check out.
        /// </returns>
        Task<VerifiedAttestation?> VerifyAttestationAsync(string credentialJson, string state);

        /// <summary>
        /// Options for signing in, naming no account.
        /// </summary>
        Task<PasskeyChallenge> CreateLoginOptionsAsync();

        /// <returns>
        /// The account the passkey belongs to, and the passkey as it is to be
        /// stored, or null when the authenticator's answer does not check out.
        /// </returns>
        Task<VerifiedAssertion?> VerifyAssertionAsync(string credentialJson, string state);
    }
}
