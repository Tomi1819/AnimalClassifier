namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Extensions;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using static Constants.RoleConstants;
    using static Constants.MessageConstants;

    public class AuthService : IAuthService
    {
        /// <summary>
        /// Carries a hash of the user's security stamp rather than the stamp
        /// itself, because anyone holding a token can read it and Identity
        /// derives one-time codes from the stamp.
        /// </summary>
        private const string SecurityStampClaimType = "security_stamp";

        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly JwtSettings jwtSettings;

        public AuthService(UserManager<ApplicationUser> userManager,
                           SignInManager<ApplicationUser> signInManager,
                           IOptions<JwtSettings> jwtOptions)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.jwtSettings = jwtOptions.Value;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            if (await userManager.FindByEmailAsync(request.Email) != null)
            {
                throw new InvalidOperationException(AlreadyRegisteredEmail);
            }

            string processedFullName = ProcessFullName(request.FullName);

            var user = new ApplicationUser
            {
                FullName = processedFullName,
                UserName = request.Email,
                Email = request.Email,
                DateRegistered = DateTime.UtcNow
            };

            (await userManager.CreateAsync(user, request.Password)).ThrowIfFailed();
            (await userManager.AddToRoleAsync(user, User)).ThrowIfFailed();

            return new RegisterResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };
        }

        public async Task<LoginResponse> LoginAsync(LogInRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException(InvalidCredentials);
            }

            // Unlike checking the password alone, this refuses a locked-out account
            // and counts a wrong password towards locking it.
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                throw new UnauthorizedAccessException(LockedOutAccount);
            }

            if (!result.Succeeded)
            {
                throw new UnauthorizedAccessException(InvalidCredentials);
            }

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(SecurityStampClaimType, await GetSecurityStampHashAsync(user))
            };

            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = GenerateJwtToken(authClaims);

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                Roles = userRoles.ToList()
            };
        }

        public async Task<bool> IsSessionValidAsync(ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);

            return user != null
                && principal.FindFirstValue(SecurityStampClaimType) == await GetSecurityStampHashAsync(user);
        }

        private async Task<string> GetSecurityStampHashAsync(ApplicationUser user)
        {
            var securityStamp = await userManager.GetSecurityStampAsync(user);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));
        }

        private JwtSecurityToken GenerateJwtToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

            return new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                expires: DateTime.UtcNow.AddHours(jwtSettings.ExpirationHours),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
        }

        private string ProcessFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return UnknownUser;
            }

            var words = fullName.Split(Space, StringSplitOptions.RemoveEmptyEntries)
                                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower());

            return string.Join(Space, words);
        }
    }
}
