namespace AnimalClassifier.Core.Configurations
{
    public class PasskeySettings
    {
        /// <summary>
        /// The relying party id passkeys are bound to. Left empty, it is taken
        /// from the host in <see cref="FrontendSettings.BaseUrl"/>, which is
        /// the right answer whenever the frontend is served from one domain.
        /// It is set explicitly to cover several, in which case it has to be a
        /// domain they all sit under: a frontend on app.example.com and an API
        /// on api.example.com share example.com.
        /// </summary>
        public string ServerDomain { get; set; } = string.Empty;
    }
}
