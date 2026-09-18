namespace AnimalClassifier.Constants
{
    public static class MessageConstants
    {
        //AnimalController
        public const string EnterSearchTerm = "Please, enter a search term.";
        public const string NoMatches = "There is no mathes yet.";

        //StatisticsController
        public const string UnknownTimeZone = "The time zone is not recognized.";

        //ServiceCollectionExtension
        public const string MissingConnectionString = "Connection string 'DefaultConnection' not found.";
        public const string MissingMLModelPath = "ML model path is not configured.";
        public const string MissingJwtSecurityKey = "JWT Secret Key is not configured.";
        public const string MissingUploadPath = "File upload path is not configured.";
        public const string OutdatedToken = "The token no longer matches the account.";
    }
}
