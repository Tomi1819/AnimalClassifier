namespace AnimalClassifier.Extensions
{
    using Microsoft.IdentityModel.JsonWebTokens;
    using System.Globalization;
    using System.Security.Claims;

    public static class ClaimsPrincipalExtension
    {
        private const string MissingUserId = "The caller is not signed in.";

        public static string? Id(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// The id of a caller who is known to be signed in, which is every
        /// caller an action marked for signed-in users is reached by. One
        /// without an id means the action was left open by mistake, and that
        /// has to fail here rather than be passed on as nobody.
        /// </summary>
        public static string RequiredId(this ClaimsPrincipal user)
            => user.Id() ?? throw new InvalidOperationException(MissingUserId);

        /// <summary>
        /// When the token the caller signed in with runs out. Every token that
        /// gets as far as a controller carries it, since one without an expiry
        /// is refused.
        /// </summary>
        public static DateTime TokenExpiration(this ClaimsPrincipal user)
            => DateTimeOffset.FromUnixTimeSeconds(
                long.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Exp)!, CultureInfo.InvariantCulture)).UtcDateTime;
    }
}

