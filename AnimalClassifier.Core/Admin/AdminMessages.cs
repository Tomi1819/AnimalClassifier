namespace AnimalClassifier.Core.Admin
{
    /// <summary>
    /// What managing users tells an administrator. Each is written to be shown
    /// as it stands.
    /// </summary>
    public static class AdminMessages
    {
        public const string OwnAccountChange = "Administrators cannot change their own account.";
        public const string UserAlreadyLocked = "The user is already locked.";
        public const string UserNotLocked = "The user is not locked.";

        /// <summary>
        /// Named in the audit log in place of an account that has since been
        /// deleted.
        /// </summary>
        public const string DeletedUser = "Deleted user";
    }
}
