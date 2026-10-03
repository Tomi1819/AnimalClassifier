namespace AnimalClassifier.Core.Identity.Passkeys
{
    using AnimalClassifier.Core.Common.Settings;
    using AnimalClassifier.Core.Configurations;

    public class PasskeySettings : ISettings
    {
        public static string SectionName => "Passkey";

        /// <summary>
        /// The relying party id passkeys are bound to. Left empty, it is taken
        /// from the host in <see cref="FrontendSettings.BaseUrl"/>, which is
        /// the right answer whenever the frontend is served from one domain.
        /// It is set explicitly to cover several, in which case it has to be a
        /// domain they all sit under: a frontend on app.example.com and an API
        /// on api.example.com share example.com.
        /// </summary>
        public string ServerDomain { get; set; } = string.Empty;

        /// <returns>
        /// The domain passkeys are bound to, or null when it is neither set
        /// here nor readable from the frontend's address.
        /// </returns>
        public string? ResolveServerDomain(FrontendSettings frontend)
        {
            if (!string.IsNullOrWhiteSpace(ServerDomain))
            {
                return ServerDomain;
            }

            return Uri.TryCreate(frontend.BaseUrl, UriKind.Absolute, out var baseUrl) ? baseUrl.Host : null;
        }
    }
}
