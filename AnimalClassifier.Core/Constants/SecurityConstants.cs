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

        /// <summary>
        /// How much longer than the authenticator's own timeout the state of a
        /// passkey ceremony stays valid. The authenticator starts counting once
        /// the browser has been handed the options, so the state is always the
        /// older of the two, and this covers the round trips either side of it.
        /// </summary>
        public static readonly TimeSpan PasskeyStateGrace = TimeSpan.FromMinutes(1);
    }
}
