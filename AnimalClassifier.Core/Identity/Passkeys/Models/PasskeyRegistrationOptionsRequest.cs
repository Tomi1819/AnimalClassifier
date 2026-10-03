namespace AnimalClassifier.Core.Identity.Passkeys.Models
{
    public class PasskeyRegistrationOptionsRequest
    {
        /// <summary>
        /// The account's password, which shows that the user themselves is
        /// the one adding the passkey.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
