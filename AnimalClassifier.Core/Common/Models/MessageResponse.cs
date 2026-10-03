namespace AnimalClassifier.Core.Common.Models
{
    /// <summary>
    /// An answer that is only a message for the user, whether it reports a
    /// refusal or confirms that something was done.
    /// </summary>
    public class MessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
