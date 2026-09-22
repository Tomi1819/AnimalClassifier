namespace AnimalClassifier.Core.DTO
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Nodes;

    /// <summary>
    /// What the authenticator produced, returned with the state the options it
    /// answers were issued with.
    /// </summary>
    public class PasskeyCredentialRequest
    {
        [Required]
        public JsonNode? Credential { get; set; }

        [Required]
        public string State { get; set; } = string.Empty;
    }
}
