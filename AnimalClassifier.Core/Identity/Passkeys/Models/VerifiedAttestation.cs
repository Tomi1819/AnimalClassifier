namespace AnimalClassifier.Core.Identity.Passkeys.Models
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// A new passkey whose authenticator's answer checked out.
    /// </summary>
    public class VerifiedAttestation
    {
        /// <summary>
        /// The account the ceremony's state names, which is the only record of
        /// whose the passkey is.
        /// </summary>
        public required string UserId { get; init; }

        public required UserPasskeyInfo Passkey { get; init; }
    }
}
