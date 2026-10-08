namespace AnimalClassifier.Core.Identity.Passkeys.Models
{
    /// <summary>
    /// The options a browser is asked to start a passkey ceremony with, and
    /// the state the ceremony has to be finished with.
    /// </summary>
    public class PasskeyChallenge
    {
        public required string OptionsJson { get; init; }

        /// <summary>
        /// Null for a ceremony that needs nothing remembered.
        /// </summary>
        public string? State { get; init; }
    }
}
