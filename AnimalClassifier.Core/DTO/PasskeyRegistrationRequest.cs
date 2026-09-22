namespace AnimalClassifier.Core.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class PasskeyRegistrationRequest : PasskeyCredentialRequest
    {
        /// <summary>
        /// What the user calls this passkey, so that they can tell it apart
        /// from the others on the list. A blank one is named for them.
        /// </summary>
        [StringLength(64)]
        public string? Name { get; set; }
    }
}
