namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Extensions;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using static Constants.MessageConstants;

    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IAccessTokenIssuer tokenIssuer;

        public AccountService(UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              IAccessTokenIssuer tokenIssuer)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.tokenIssuer = tokenIssuer;
        }

        public async Task<LoginResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await FindUserAsync(userId);

            await ConfirmPasswordAsync(user, request.CurrentPassword);

            (await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)).ThrowIfFailed();

            return await tokenIssuer.IssueAsync(user);
        }

        public async Task<LoginResponse> SignOutOtherSessionsAsync(string userId)
        {
            var user = await FindUserAsync(userId);

            (await userManager.UpdateSecurityStampAsync(user)).ThrowIfFailed();

            return await tokenIssuer.IssueAsync(user);
        }

        private async Task<ApplicationUser> FindUserAsync(string userId) =>
            await userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException(UserNotFound);

        // Checked apart from the change itself, which would only report a
        // mismatch, so that a wrong guess is counted towards a lockout.
        private async Task ConfirmPasswordAsync(ApplicationUser user, string password)
        {
            var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                throw new InvalidOperationException(LockedOutAccount);
            }

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(IncorrectCurrentPassword);
            }
        }
    }
}
