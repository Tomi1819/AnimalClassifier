namespace AnimalClassifier.Core.Identity.EmailConfirmation.Models
{
    public class ConfirmEmailRequest
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The token from the emailed link, still encoded as it travelled.
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
