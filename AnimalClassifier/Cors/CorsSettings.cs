namespace AnimalClassifier.Cors
{
    using AnimalClassifier.Core.Common.Settings;

    public class CorsSettings : ISettings
    {
        /// <summary>
        /// The one policy every endpoint answers browsers by.
        /// </summary>
        public const string PolicyName = "CorsPolicy";

        public static string SectionName => "Cors";

        /// <summary>
        /// The origins a browser may call the API from, which is wherever the
        /// frontend is served. Left empty, no browser on another origin can.
        /// </summary>
        public string[] AllowedOrigins { get; set; } = [];
    }
}
