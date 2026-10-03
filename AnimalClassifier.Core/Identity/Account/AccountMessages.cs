namespace AnimalClassifier.Core.Identity.Account
{
    /// <summary>
    /// What the changes a user makes to their own account tell them. Each is
    /// written to be shown as it stands.
    /// </summary>
    public static class AccountMessages
    {
        public const string UnchangedPassword = "The new password has to be different from the current one.";
        public const string EmptyFullName = "Please enter a name.";
        public const string AdministratorAccountDeletion = "Administrators cannot delete their account. Another administrator has to revoke the role first.";
    }
}
