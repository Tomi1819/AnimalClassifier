namespace AnimalClassifier.Core.Identity.Authentication
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.EmailConfirmation;
    using Microsoft.AspNetCore.Identity;
    using System.Globalization;
    using System.Security.Claims;
    using static AnimalClassifier.Core.Identity.Authentication.AuthenticationMessages;
    using static AnimalClassifier.Core.Identity.RoleConstants;

    public class AuthService : IAuthService
    {
        /// <summary>
        /// The longest email an account can have, which is the length of the
        /// column Identity keeps it in.
        /// </summary>
        public const int MaxEmailLength = 256;

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasswordSignInChecker passwordSignInChecker;
        private readonly IAccessTokenIssuer tokenIssuer;
        private readonly IEmailConfirmationService emailConfirmationService;
        private readonly TimeProvider timeProvider;

        public AuthService(UserManager<ApplicationUser> userManager,
                           IPasswordSignInChecker passwordSignInChecker,
                           IAccessTokenIssuer tokenIssuer,
                           IEmailConfirmationService emailConfirmationService,
                           TimeProvider timeProvider)
        {
            this.userManager = userManager;
            this.passwordSignInChecker = passwordSignInChecker;
            this.tokenIssuer = tokenIssuer;
            this.emailConfirmationService = emailConfirmationService;
            this.timeProvider = timeProvider;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            if (request.Email.Length > MaxEmailLength)
            {
                throw new RequestRefusedException(string.Format(CultureInfo.InvariantCulture, EmailTooLong, MaxEmailLength));
            }

            if (await userManager.FindByEmailAsync(request.Email) != null)
            {
                throw new RequestRefusedException(AlreadyRegisteredEmail);
            }

            var user = new ApplicationUser
            {
                FullName = NameAtRegistration(request.FullName),
                UserName = request.Email,
                Email = request.Email,
                DateRegistered = timeProvider.GetUtcNow().UtcDateTime
            };

            (await userManager.CreateAsync(user, request.Password)).ThrowIfFailed();
            (await userManager.AddToRoleAsync(user, User)).ThrowIfFailed();
            await emailConfirmationService.SendLinkAsync(user);

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
            var result = await passwordSignInChecker.CheckAsync(user, request.Password);

            if (result == PasswordSignInResult.LockedOut)
            {
                throw new AuthenticationFailedException(LockedOutAccount);
            }

            if (result != PasswordSignInResult.Succeeded)
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

            return name.Length == 0 ? AccountName.Unknown : AccountName.Capitalise(name);
        }
    }
}
