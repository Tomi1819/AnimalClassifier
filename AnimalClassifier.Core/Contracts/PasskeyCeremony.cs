namespace AnimalClassifier.Core.Contracts
{
    /// <summary>
    /// The two halves of passkey use: registering a new credential, and
    /// proving possession of one that is already registered.
    /// </summary>
    public enum PasskeyCeremony
    {
        Attestation,
        Assertion
    }
}
