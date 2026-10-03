namespace AnimalClassifier.Core.Identity.Authentication
{
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    public class AccessTokenIssuer : IAccessTokenIssuer
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly JwtSettings jwtSettings;

        public AccessTokenIssuer(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtOptions)
        {
            this.userManager = userManager;
            this.jwtSettings = jwtOptions.Value;
        }

        public Task<LoginResponse> IssueAsync(ApplicationUser user) =>
            CreateAsync(user, DateTime.UtcNow.AddHours(jwtSettings.ExpirationHours));

        public Task<LoginResponse> ReissueAsync(ApplicationUser user, DateTime expiration) =>
            CreateAsync(user, expiration);

        private async Task<LoginResponse> CreateAsync(ApplicationUser user, DateTime expiration)
        {
            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(SecurityStampClaim.Type, SecurityStampClaim.From(await userManager.GetSecurityStampAsync(user)))
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                expires: expiration,
                claims: claims,
                signingCredentials: new SigningCredentials(jwtSettings.CreateSigningKey(), SecurityAlgorithms.HmacSha256));

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                Roles = roles.ToList()
            };
        }
    }
}
