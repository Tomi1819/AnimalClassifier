namespace AnimalClassifier.Core.Identity.Authentication
{
    using AnimalClassifier.Core.Common.Settings;
    using Microsoft.IdentityModel.Tokens;
    using System.ComponentModel.DataAnnotations;
    using System.Text;

    public class JwtSettings : ISettings
    {
        /// <summary>
        /// The shortest key tokens can be signed with. HMAC-SHA256 refuses a
        /// key of fewer than 256 bits, and every character is at least a byte.
        /// </summary>
        public const int MinSecretKeyLength = 32;

        public static string SectionName => "Jwt";

        /// <summary>
        /// A secret, so it belongs in user secrets or an environment variable
        /// rather than in appsettings.json.
        /// </summary>
        [Required]
        [MinLength(MinSecretKeyLength)]
        public string SecretKey { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ExpirationHours { get; set; }

        /// <summary>
        /// The key tokens are signed with. Issuing and validating both build
        /// it here, so a token is never checked against a key made a
        /// different way.
        /// </summary>
        public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SecretKey));
    }
}
