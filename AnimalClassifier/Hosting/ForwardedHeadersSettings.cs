namespace AnimalClassifier.Hosting
{
    using AnimalClassifier.Core.Common.Settings;

    public class ForwardedHeadersSettings : ISettings
    {
        public static string SectionName => "ForwardedHeaders";

        /// <summary>
        /// The addresses of the reverse proxies whose word on the caller's
        /// address and scheme is taken. One on this machine is taken without
        /// being named. Anyone else could put whatever address they liked in
        /// the headers, and slip every limit kept per address.
        /// </summary>
        public string[] KnownProxies { get; set; } = [];
    }
}
