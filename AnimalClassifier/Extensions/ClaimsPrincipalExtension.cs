namespace AnimalClassifier.Extensions
{
    using Microsoft.IdentityModel.JsonWebTokens;
    using System.Globalization;
    using System.Security.Claims;
    public static class ClaimsPrincipalExtension
    {
        public static string? Id(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier);

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

