namespace AnimalClassifier.Core.Identity.Passkeys.Models
{
    using AnimalClassifier.Core.Data.Entities;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// A passkey proven to be held, whose authenticator's answer checked out.
    /// </summary>
    public class VerifiedAssertion
    {
        public required ApplicationUser User { get; init; }

        /// <summary>
        /// The passkey with its counter moved on, as it is to be stored.
        /// </summary>
        public required UserPasskeyInfo Passkey { get; init; }
    }
}
