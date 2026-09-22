namespace AnimalClassifier.Core.Contracts
{
    /// <summary>
    /// Carries the state of a passkey ceremony across the two requests it
    /// spans: one to hand the browser its options, another to take back what
    /// the authenticator produced.
    ///
    /// Identity's own sign-in manager keeps this state in an authentication
    /// cookie. This API issues bearer tokens and is served from a different
    /// origin than the frontend, so it keeps the state here instead, and with
    /// it the three guarantees the cookie was providing.
    ///
    /// <list type="bullet">
    /// <item><description>
    /// Integrity. The state names the account a new passkey is registered to,
    /// and nothing else in an attestation does. A caller who could edit it
    /// would have their own authenticator registered against any account they
    /// named, so it is encrypted and signed before it leaves the server.
    /// </description></item>
    /// <item><description>
    /// Separation. A state made for one ceremony cannot be presented as the
    /// other, because each is protected under its own purpose.
    /// </description></item>
    /// <item><description>
    /// Expiry. A state outlives its ceremony only by the margin the round
    /// trips need, which bounds how long a captured one is worth replaying.
    /// </description></item>
    /// </list>
    ///
    /// Ownership is the caller's to check: whoever completes an attestation
    /// must confirm the account in the state is the one they are signed in as.
    /// </summary>
    public interface IPasskeyStateProtector
    {
        string Protect(PasskeyCeremony ceremony, string state);

        /// <summary>
        /// Recovers a state this server issued for the same ceremony.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The state was not issued here, was made for the other ceremony, was
        /// tampered with, or has expired. These are not told apart, because a
        /// caller can do nothing about any of them but start again.
        /// </exception>
        string Unprotect(PasskeyCeremony ceremony, string state);
    }
}
