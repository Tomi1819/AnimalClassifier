namespace AnimalClassifier.Core.Identity.Authentication
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using static AnimalClassifier.Core.Identity.Authentication.AuthenticationMessages;
    using static AnimalClassifier.Core.Identity.RoleConstants;
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
                throw new RequestRefusedException(AlreadyRegisteredEmail);
            }

            var user = new ApplicationUser
            {
                FullName = NameAtRegistration(request.FullName),
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

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new AuthenticationFailedException(InvalidCredentials);
            }

            // Unlike checking the password alone, this refuses a locked-out account
            // and counts a wrong password towards locking it.
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                throw new AuthenticationFailedException(LockedOutAccount);
            }

            if (!result.Succeeded)
            {
                throw new AuthenticationFailedException(InvalidCredentials);
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

        // Capitalised here and nowhere else: a name changed later keeps its
        // letters as typed.
        private static string NameAtRegistration(string fullName)
        {
            var name = AccountName.Tidy(fullName);

            return name.Length == 0 ? UnknownUser : AccountName.Capitalise(name);
        }
    }
}
