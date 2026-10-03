namespace AnimalClassifier.Core.Identity.Account.Models
{
    /// <summary>
    /// What the signed-in user's account says about them.
    /// </summary>
    public class AccountProfile
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateRegistered { get; set; }
    }
}
