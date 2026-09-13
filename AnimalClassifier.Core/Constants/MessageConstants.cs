namespace AnimalClassifier.Core.Constants
{
    public static class MessageConstants
    {
        //AuthService
        public const string AlreadyRegisteredEmail = "This email is already registered.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string LockedOutAccount = "This account is locked.";
        public const string UnknownUser = "Unknown user";
        public const string Space = " ";

        //RecognitionService
        public const string ImageNotFound = "Image not found.";
        public const string FailedPrediction = "Prediction failed.";
        public const string DefaultExtension = ".jpg";
    }
}
