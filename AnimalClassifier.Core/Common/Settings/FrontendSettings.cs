namespace AnimalClassifier.Core.Common.Settings
{
    using System.ComponentModel.DataAnnotations;

    public class FrontendSettings : ISettings
    {
        public static string SectionName => "Frontend";

        /// <summary>
        /// Where the frontend is served from. Configured rather than taken
        /// from the incoming request, whose host header is whatever the caller
        /// put in it, because the emailed links are built from this. A link to
        /// nowhere would only be found by the user who cannot get back into
        /// their account, so it is required.
        /// </summary>
        [Required]
        [Url]
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// The page that receives a password reset token, which has to match
        /// the frontend.
        /// </summary>
        [Required]
        public string ResetPasswordPath { get; set; } = string.Empty;
    }
}
