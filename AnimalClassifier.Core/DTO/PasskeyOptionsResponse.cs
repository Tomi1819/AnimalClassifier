namespace AnimalClassifier.Core.DTO
{
    using System.Text.Json.Nodes;

    /// <summary>
    /// What the browser needs to start a passkey ceremony, and the state that
    /// has to come back with the result for it to be finished.
    /// </summary>
    public class PasskeyOptionsResponse
    {
        /// <summary>
        /// The WebAuthn options, passed on to the browser untouched.
        /// </summary>
        public JsonNode? Options { get; set; }

        /// <summary>
        /// Protected by the server and meaningless to the caller, whose only
        /// job is to hand it back.
        /// </summary>
        public string State { get; set; } = string.Empty;
    }
}
