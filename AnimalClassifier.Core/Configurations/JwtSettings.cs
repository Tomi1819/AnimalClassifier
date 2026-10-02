namespace AnimalClassifier.Core.Configurations
{
    using Microsoft.IdentityModel.Tokens;
    using System.Text;

    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationHours { get; set; }

        /// <summary>
        /// The key tokens are signed with. Issuing and validating both build
        /// it here, so a token is never checked against a key made a
        /// different way.
        /// </summary>
        public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SecretKey));
    }
}
