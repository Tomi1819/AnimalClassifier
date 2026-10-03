namespace AnimalClassifier.Core.Identity.Passkeys
{
    /// <summary>
    /// What registering, using and removing a passkey tell the user. Each is
    /// written to be shown as it stands.
    /// </summary>
    public static class PasskeyMessages
    {
        public const string ExpiredPasskeyCeremony = "This passkey request is no longer valid. Please try again.";
        public const string RejectedPasskey = "This passkey could not be registered. Please try again.";
        public const string InvalidPasskey = "That passkey was not recognised.";
        public const string PasskeyNotFound = "The passkey does not exist.";
    }
}
