namespace AnimalClassifier.Core.Constants
{
    public static class MessageConstants
    {
        //AuthService
        public const string UnknownUser = "Unknown user";

        //AdminService
        public const string OwnAccountChange = "Administrators cannot change their own account.";
        public const string UserAlreadyLocked = "The user is already locked.";
        public const string UserNotLocked = "The user is not locked.";
        public const string DeletedUser = "Deleted user";

        //RecognitionService
        public const string ImageNotFound = "Image not found.";
        public const string FailedPrediction = "Prediction failed.";
        public const string DefaultExtension = ".jpg";
    }
}
