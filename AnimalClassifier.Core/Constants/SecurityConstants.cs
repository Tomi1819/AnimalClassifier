namespace AnimalClassifier.Core.Constants
{
    public static class SecurityConstants
    {
        /// <summary>
        /// How long a password reset link stays valid. It is emailed, so it
        /// has to outlive the delivery while still expiring long before the
        /// message is forgotten in an inbox. Identity's own default is a day,
        /// and the email quotes this figure, so both read it from here.
        /// </summary>
        public static readonly TimeSpan PasswordResetTokenLifespan = TimeSpan.FromHours(1);
    }
}
