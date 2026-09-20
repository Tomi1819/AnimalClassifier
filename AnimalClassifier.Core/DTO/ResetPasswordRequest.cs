namespace AnimalClassifier.Core.DTO
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The token from the emailed link, still in the encoded form it
        /// travelled in.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}
