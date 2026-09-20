namespace AnimalClassifier.Core.Configurations
{
    public class FrontendSettings
    {
        /// <summary>
        /// Where the frontend is served from, used to build the links that go
        /// out by email. It is configured rather than taken from the incoming
        /// request, because a request's host header is whatever the caller
        /// chose to put in it, and a reset link has to point home.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// The page that takes the reset token, relative to <see cref="BaseUrl"/>.
        /// </summary>
        public string ResetPasswordPath { get; set; } = string.Empty;
    }
}
