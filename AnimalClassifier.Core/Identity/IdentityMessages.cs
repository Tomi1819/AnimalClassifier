namespace AnimalClassifier.Core.Identity
{
    /// <summary>
    /// What more than one part of identity tells the user. Each is written to
    /// be shown as it stands.
    /// </summary>
    public static class IdentityMessages
    {
        public const string UserNotFound = "The user does not exist.";

        /// <summary>
        /// Formatted with the longest name allowed.
        /// </summary>
        public const string FullNameTooLong = "The name can be at most {0} characters long.";
    }
}
