namespace AnimalClassifier.Constants
{
    public static class MessageConstants
    {
        //AuthController
        public const string PasswordResetEmailSent = "If that address has an account, a reset link is on its way.";
        public const string PasswordChanged = "Your password has been changed. Please sign in.";

        //StatisticsController
        public const string UnknownTimeZone = "The time zone is not recognized.";

        //ClaimsPrincipalExtension
        public const string MissingUserId = "The caller is not signed in.";
    }
}
