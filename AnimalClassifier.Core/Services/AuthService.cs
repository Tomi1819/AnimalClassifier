namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Extensions;
    using AnimalClassifier.Core.Services.Helpers;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using static Constants.RoleConstants;
    using static Constants.MessageConstants;

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IAccessTokenIssuer tokenIssuer;

        public AuthService(UserManager<ApplicationUser> userManager,
                           SignInManager<ApplicationUser> signInManager,
                           IAccessTokenIssuer tokenIssuer)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.tokenIssuer = tokenIssuer;
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

            return await tokenIssuer.IssueAsync(user);
        }

        public async Task<bool> IsSessionValidAsync(ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);

            return user != null
                && principal.FindFirstValue(SecurityStampClaim.Type)
                    == SecurityStampClaim.From(await userManager.GetSecurityStampAsync(user));
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
