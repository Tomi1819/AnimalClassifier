namespace AnimalClassifier.Core.Identity.EmailConfirmation
{
    /// <summary>
    /// What confirming an email tells the user. Each is written to be shown as
    /// it stands.
    /// </summary>
    public static class EmailConfirmationMessages
    {
        public const string EmailConfirmed = "Your email is confirmed.";
        public const string EmailAlreadyConfirmed = "Your email is already confirmed.";
        public const string EmailConfirmationLinkSent = "A link to confirm your email is on its way.";
        public const string InvalidEmailConfirmationLink = "This link is no longer valid. Please ask for a new one.";
    }
}
