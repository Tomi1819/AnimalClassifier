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

        //ServiceCollectionExtension
        public const string MissingConnectionString = "Connection string 'DefaultConnection' not found.";
        public const string MissingMLModelPath = "ML model path is not configured.";
        public const string MissingJwtSecurityKey = "JWT Secret Key is not configured.";
        public const string MissingUploadPath = "File upload path is not configured.";
        public const string MissingEmailSettings = "Email host and sender address are not configured.";
        public const string MissingFrontendBaseUrl = "Frontend base URL is not configured.";
        public const string OutdatedToken = "The token no longer matches the account.";
    }
}
