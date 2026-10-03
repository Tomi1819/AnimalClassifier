namespace AnimalClassifier.Constants
{
    public static class MessageConstants
    {
        //AnimalController
        public const string EnterSearchTerm = "Please, enter a search term.";
        public const string NoMatches = "There is no mathes yet.";

        //AuthController
        public const string PasswordResetEmailSent = "If that address has an account, a reset link is on its way.";
        public const string PasswordChanged = "Your password has been changed. Please sign in.";
        public const string TooManyRequests = "Too many attempts. Please wait a few minutes and try again.";

        //StatisticsController
        public const string UnknownTimeZone = "The time zone is not recognized.";

        //ClaimsPrincipalExtension
        public const string MissingUserId = "The caller is not signed in.";
    }
}
