namespace AnimalClassifier.Core.Identity.Passwords
{
    /// <summary>
    /// What a password has to be. Identity is configured from it, and what
    /// quotes it reads it from here too.
    /// </summary>
    public static class PasswordPolicy
    {
        /// <summary>
        /// The shortest password an account can be given, whether at
        /// registration, by a change or by a reset. The frontend's fields
        /// match it. A password set before it was raised keeps working.
        /// </summary>
        public const int MinLength = 8;
    }
}
