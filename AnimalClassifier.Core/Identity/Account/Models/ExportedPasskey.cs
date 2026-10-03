namespace AnimalClassifier.Core.Identity.Account.Models
{
    /// <summary>
    /// A passkey as its owner knows it. The credential itself is left out, as
    /// it means nothing outside signing in.
    /// </summary>
    public class ExportedPasskey
    {
        public string Name { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; }
    }
}
