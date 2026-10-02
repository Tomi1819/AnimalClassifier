namespace AnimalClassifier.Core.Constants
{
    public static class MessageConstants
    {
        //AuthService
        public const string AlreadyRegisteredEmail = "This email is already registered.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string LockedOutAccount = "This account is locked.";
        public const string UnknownUser = "Unknown user";
        public const string PasswordIsEmail = "The password cannot be your email address.";

        //AdminService
        public const string UserNotFound = "The user does not exist.";
        public const string OwnAccountChange = "Administrators cannot change their own account.";
        public const string UserAlreadyLocked = "The user is already locked.";
        public const string UserNotLocked = "The user is not locked.";
        public const string DeletedUser = "Deleted user";

        //PasswordResetService
        public const string InvalidPasswordResetLink = "This link is no longer valid. Please ask for a new one.";

        //AccountService
        public const string IncorrectCurrentPassword = "The current password is incorrect.";
        public const string TooManyPasswordAttempts = "Too many password attempts. Please wait a few minutes and try again.";
        public const string UnchangedPassword = "The new password has to be different from the current one.";
        public const string EmptyFullName = "Please enter a name.";
        public const string FullNameTooLong = "The name can be at most {0} characters long.";
        public const string AdministratorAccountDeletion ="Administrators cannot delete their account. Another administrator has to revoke the role first.";

        //PasskeyService
        public const string ExpiredPasskeyCeremony = "This passkey request is no longer valid. Please try again.";
        public const string RejectedPasskey = "This passkey could not be registered. Please try again.";
        public const string InvalidPasskey = "That passkey was not recognised.";
        public const string PasskeyNotFound = "The passkey does not exist.";

        //RecognitionService
        public const string ImageNotFound = "Image not found.";
        public const string FailedPrediction = "Prediction failed.";
        public const string DefaultExtension = ".jpg";
    }
}
