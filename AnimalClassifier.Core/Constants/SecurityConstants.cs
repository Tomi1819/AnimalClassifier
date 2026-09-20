namespace AnimalClassifier.Core.Constants
{
    public static class SecurityConstants
    {
        /// <summary>
        /// How long a token from Identity's default providers stays valid. The
        /// password reset link is the only one in use, and it is emailed, so it
        /// has to outlive the delivery while still expiring long before the
        /// message is forgotten in an inbox. Identity's own default is a day.
        /// The email quotes this figure, so both read it from here.
        /// </summary>
        public static readonly TimeSpan PasswordResetTokenLifespan = TimeSpan.FromHours(1);
    }
}
