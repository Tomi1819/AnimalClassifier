namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly JwtSettings jwtSettings;

        public AuthService(UserManager<ApplicationUser> userManager,
                           RoleManager<IdentityRole> roleManager,
                           IOptions<JwtSettings> jwtOptions)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.jwtSettings = jwtOptions.Value;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            if (await userManager.FindByEmailAsync(request.Email) != null)
            {
                throw new InvalidOperationException("This email is already registered.");
            }

            string processedFullName = ProcessFullName(request.FullName);
            string generatedUserName = GenerateUserName(request.FullName, request.Email);

            var user = new ApplicationUser
            {
                FullName = processedFullName,
                UserName = generatedUserName,
                Email = request.Email,
                DateRegistered = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("User creation failed.");
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            await userManager.AddToRoleAsync(user, "User");

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
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
                Expiration = token.ValidTo
            };
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
                return "Unknown User";
            }

            var words = fullName.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower());

            return string.Join(" ", words);
        }

        private string GenerateUserName(string fullName, string email)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return email.Split('@')[0].ToLower();
            }

            var words = fullName.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            string baseUserName = words.Length > 1 ? $"{words[0]}.{words[^1]}" : words[0];

            baseUserName = Regex.Replace(baseUserName, @"[^a-zA-Z0-9._]", "").ToLower();

            return baseUserName;
        }
    }
}
