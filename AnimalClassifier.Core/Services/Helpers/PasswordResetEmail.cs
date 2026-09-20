namespace AnimalClassifier.Core.Services.Helpers
{
    using System.Net;

    /// <summary>
    /// The wording of the password reset email, kept apart from the service
    /// that sends it so that the two change for different reasons.
    /// </summary>
    public static class PasswordResetEmail
    {
        public const string Subject = "Reset your Animal Classifier password";

        /// <summary>
        /// Both the name and the link are encoded: the name is whatever the
        /// user typed when registering, and the link carries a query string
        /// whose separators are not valid in an attribute as they stand.
        /// </summary>
        public static string BuildBody(string name, string link, TimeSpan lifespan) =>
            $"""
            <p>Hello {WebUtility.HtmlEncode(name)},</p>
            <p>Someone asked to reset the password of your Animal Classifier account.
               You can choose a new one here:</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Reset your password</a></p>
            <p>The link works once and stops working after {Describe(lifespan)}.</p>
            <p>If this was not you, ignore this email. Your password has not changed.</p>
            """;

        private static string Describe(TimeSpan lifespan) =>
            lifespan.TotalHours == 1 ? "an hour" : $"{lifespan.TotalHours:0} hours";
    }
}
