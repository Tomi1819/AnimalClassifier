namespace AnimalClassifier.Core.Identity.EmailConfirmation
{
    using System.Net;

    public static class EmailConfirmationEmail
    {
        public const string Subject = "Confirm your Animal Classifier email";

        /// <summary>
        /// Both the name and the link are encoded, as in the password reset
        /// email: the name is whatever the user typed when registering.
        /// </summary>
        public static string BuildBody(string name, string link) =>
            $"""
            <p>Hello {WebUtility.HtmlEncode(name)},</p>
            <p>Please confirm that this is the email of your Animal Classifier account:</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Confirm your email</a></p>
            <p>The link stops working after {EmailedLink.DescribeLifespan()}. You can ask for a new one
               on your account page.</p>
            <p>If you did not create this account, you can ignore this email.</p>
            """;
    }
}
