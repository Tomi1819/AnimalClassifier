namespace AnimalClassifier.Core.Identity.Passwords
{
    /// <summary>
    /// What a password has to be, and how long a link to replace a forgotten
    /// one lasts. Identity is configured from these, and what quotes them, such
    /// as the reset email, reads them from here too.
    /// </summary>
    public static class PasswordPolicy
    {
        /// <summary>
        /// The shortest password an account can be given, whether at
        /// registration, by a change or by a reset. The frontend's fields
        /// match it. A password set before it was raised keeps working.
        /// </summary>
        public const int MinLength = 8;

        /// <summary>
        /// How long a password reset link stays valid. It is emailed, so it
        /// has to outlive the delivery while still expiring long before the
        /// message is forgotten in an inbox. Identity's own default is a day.
        /// </summary>
        public static readonly TimeSpan ResetTokenLifespan = TimeSpan.FromHours(1);
    }
}
