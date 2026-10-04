namespace AnimalClassifier.Core.Identity.Account.Models
{
    /// <summary>
    /// What the signed-in user's account says about them.
    /// </summary>
    public class AccountProfile
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Whether the email has been confirmed by the link mailed to it.
        /// </summary>
        public bool EmailConfirmed { get; set; }

        public DateTime DateRegistered { get; set; }
    }
}
