namespace AnimalClassifier.Core.Identity
{
    using AnimalClassifier.Core.Common.Settings;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.WebUtilities;
    using System.Text;

    /// <summary>
    /// A link emailed to an address, which only someone who can read its mail
    /// can follow: to confirm the address, or to reset a forgotten password.
    /// Each takes the address and one of Identity's tokens to a page of the
    /// frontend.
    /// </summary>
    public static class EmailedLink
    {
        private const string EmailParameter = "email";
        private const string TokenParameter = "token";

        /// <summary>
        /// How long a link stays valid. It is emailed, so it has to outlive
        /// the delivery while still expiring long before the message is
        /// forgotten in an inbox. Identity's own default is a day.
        /// </summary>
        public static readonly TimeSpan Lifespan = TimeSpan.FromHours(1);

        /// <summary>
        /// The token travels in a query string, and the form Identity hands it
        /// over in contains characters that would not survive the journey, so
        /// it is encoded on the way out and decoded by <see cref="DecodeToken"/>.
        /// </summary>
        /// <param name="path">The frontend's page that receives the token.</param>
        public static string Build(FrontendSettings frontend, string path, string email, string token)
        {
            var query = QueryString.Create(new Dictionary<string, string?>
            {
                [EmailParameter] = email,
                [TokenParameter] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
            });

            return $"{frontend.BaseUrl.TrimEnd('/')}{path}{query}";
        }

        /// <returns>
        /// The token as Identity issued it, or null for one mangled on its
        /// way, which is no longer a token and which Identity should never see.
        /// </returns>
        public static string? DecodeToken(string token)
        {
            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// How long a link lasts, as an email tells the user.
        /// </summary>
        public static string DescribeLifespan() =>
            Lifespan.TotalHours == 1 ? "an hour" : $"{Lifespan.TotalHours:0} hours";
    }
}
