namespace AnimalClassifier.Core.Identity.Account.Models
{
    /// <summary>
    /// The account itself, as its owner's copy of their data holds it.
    /// </summary>
    public class ExportedAccount
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateRegistered { get; set; }

        public IEnumerable<ExportedPasskey> Passkeys { get; set; } = [];
    }
}
