namespace AnimalClassifier.Core.Configurations
{
    public class FrontendSettings
    {
        /// <summary>
        /// Where the frontend is served from. Configured rather than taken
        /// from the incoming request, whose host header is whatever the caller
        /// put in it, because the emailed links are built from this.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        public string ResetPasswordPath { get; set; } = string.Empty;
    }
}
