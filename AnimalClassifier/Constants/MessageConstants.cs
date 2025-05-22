namespace AnimalClassifier.Constants
{
    public static class MessageConstants
    {
        //AnimalController
        public const string EnterSearchTerm = "Please, enter a search term.";
        public const string NoMatches = "There is no mathes yet.";

        //ServiceCollectionExtension
        public const string MissingConnectionString = "Connection string 'DefaultConnection' not found.";
        public const string MissingMLModelPath = "ML model path is not configured.";
        public const string MissingJwtSecurityKey = "JWT Secret Key is not configured.";
    }
}
