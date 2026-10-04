namespace AnimalClassifier.Core.Identity.Account
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Core.Identity.SecurityAlerts;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using static AnimalClassifier.Core.Identity.Account.AccountMessages;

    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasswordConfirmer passwordConfirmer;
        private readonly IAccessTokenIssuer tokenIssuer;
        private readonly ISecurityAlertSender securityAlertSender;

        public AccountService(UserManager<ApplicationUser> userManager,
                              IPasswordConfirmer passwordConfirmer,
                              IAccessTokenIssuer tokenIssuer,
                              ISecurityAlertSender securityAlertSender)
        {
            this.userManager = userManager;
            this.passwordConfirmer = passwordConfirmer;
            this.tokenIssuer = tokenIssuer;
            this.securityAlertSender = securityAlertSender;
        }

        public async Task<AccountProfile> GetProfileAsync(string userId) =>
            ToProfile(await userManager.GetByIdAsync(userId));

        public async Task<AccountProfile> ChangeNameAsync(string userId, ChangeNameRequest request)
        {
            var user = await userManager.GetByIdAsync(userId);

            user.FullName = TidyFullName(request.FullName);
            (await userManager.UpdateAsync(user)).ThrowIfFailed();

            return ToProfile(user);
        }

        public async Task<LoginResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await userManager.GetByIdAsync(userId);

            // Checked apart from the change itself, which would only report a
            // mismatch, so that the guess is counted.
            await passwordConfirmer.ConfirmAsync(user, request.CurrentPassword);

            // Only once the current password is confirmed, so the answer says
            // nothing to someone guessing at it.
            if (request.NewPassword == request.CurrentPassword)
            {
                throw new RequestRefusedException(UnchangedPassword);
            }

            (await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)).ThrowIfFailed();
            await securityAlertSender.PasswordChangedAsync(user);

            return await tokenIssuer.IssueAsync(user);
        }

        public async Task<LoginResponse> SignOutOtherSessionsAsync(string userId, DateTime sessionExpiration)
        {
            var user = await userManager.GetByIdAsync(userId);

            (await userManager.UpdateSecurityStampAsync(user)).ThrowIfFailed();
            await securityAlertSender.OtherSessionsSignedOutAsync(user);

            return await tokenIssuer.ReissueAsync(user, sessionExpiration);
        }

        // Unlike at registration, the letters are left as typed; only the
        // spacing around and between the words is tidied.
        private static string TidyFullName(string fullName)
        {
            var name = AccountName.Tidy(fullName);

            if (name.Length == 0)
            {
                throw new RequestRefusedException(EmptyFullName);
            }

            return name;
        }

        private static AccountProfile ToProfile(ApplicationUser user) => new()
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            DateRegistered = user.DateRegistered
        };
    }
}
