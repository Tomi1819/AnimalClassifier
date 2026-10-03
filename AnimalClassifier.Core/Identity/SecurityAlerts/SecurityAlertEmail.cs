namespace AnimalClassifier.Core.Identity.SecurityAlerts
{
    using System.Net;

    /// <summary>
    /// The emails that tell a user how their account is signed in to has
    /// changed. Each says what happened and shares one ending on what to do if
    /// it was not them. None of them carries a link, so that a genuine alert
    /// never looks like the phishing it warns about.
    /// </summary>
    public static class SecurityAlertEmail
    {
        public const string PasswordChangedSubject = "Your Animal Classifier password was changed";
        public const string PasskeyAddedSubject = "A passkey was added to your Animal Classifier account";
        public const string PasskeyRemovedSubject = "A passkey was removed from your Animal Classifier account";
        public const string OtherSessionsSignedOutSubject = "Your Animal Classifier account was signed out on other devices";

        public static string BuildPasswordChangedBody(string name) =>
            BuildBody(name, "The password of your Animal Classifier account has just been changed.");

        public static string BuildPasskeyAddedBody(string name, string passkeyName) =>
            BuildBody(name, $"A passkey named \"{WebUtility.HtmlEncode(passkeyName)}\" has just been added " +
                            "to your Animal Classifier account. It can sign in without your password.");

        public static string BuildPasskeyRemovedBody(string name, string passkeyName) =>
            BuildBody(name, $"The passkey named \"{WebUtility.HtmlEncode(passkeyName)}\" has just been removed " +
                            "from your Animal Classifier account.");

        public static string BuildOtherSessionsSignedOutBody(string name) =>
            BuildBody(name, "Your Animal Classifier account has just been signed out on every device " +
                            "but the one that asked for it.");

        /// <summary>
        /// The name is encoded, being whatever the user typed. What happened is
        /// written by the builders above, which encode anything the user typed
        /// into it themselves.
        /// </summary>
        private static string BuildBody(string name, string whatHappened) =>
            $"""
            <p>Hello {WebUtility.HtmlEncode(name)},</p>
            <p>{whatHappened}</p>
            <p>If this was you, there is nothing more to do.</p>
            <p>If it was not, someone else may be signed in to your account. Choose a new
               password with "Forgot your password?" on the sign-in page, which signs out
               every device, then remove any passkey you do not recognise on your account
               page.</p>
            """;
    }
}
