namespace AnimalClassifier.Core.DTO
{
    /// <summary>
    /// One of the passkeys registered to an account, as its owner sees it.
    /// </summary>
    public class PasskeySummary
    {
        /// <summary>
        /// The credential id, in the base64url form it travels in.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; }
    }
}
