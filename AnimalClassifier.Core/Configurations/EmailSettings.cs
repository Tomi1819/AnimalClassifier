namespace AnimalClassifier.Core.Configurations
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        /// <summary>
        /// Left empty for a server that accepts mail without credentials, such
        /// as a local relay.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// A secret, so it belongs in user secrets or an environment variable
        /// rather than in appsettings.json.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// The address the messages come from. Providers deliver reliably only
        /// for a domain they have been given permission to send for.
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        public string SenderName { get; set; } = string.Empty;
    }
}
