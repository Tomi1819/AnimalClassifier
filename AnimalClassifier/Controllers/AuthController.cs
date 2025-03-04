namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration configuration;

        public AuthController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            if (await userManager.FindByEmailAsync(request.Email) != null)
            {
                return BadRequest(new { message = "This email is already registered." });
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
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            await userManager.AddToRoleAsync(user, "User");

            return Ok(new RegisterResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LogInRequest logInRequest)
        {
            if (logInRequest == null)
            {
                return BadRequest("Invalid request.");
            }

            var user = await userManager.FindByEmailAsync(logInRequest.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, logInRequest.Password))
            {
                return Unauthorized("Invalid email or password.");
            }

            var userRoles = await userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = GenerateJwtToken(authClaims);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        private JwtSecurityToken GenerateJwtToken(List<Claim> authClaims)
        {
            var secretKey = configuration["Jwt:SecretKey"];
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                expires: DateTime.UtcNow.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
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
