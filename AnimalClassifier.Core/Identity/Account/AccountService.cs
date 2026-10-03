namespace AnimalClassifier.Core.Identity.Account
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Core.Identity.SecurityAlerts;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using static AnimalClassifier.Core.Identity.Account.AccountMessages;
    using static AnimalClassifier.Core.Identity.RoleConstants;

    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasswordConfirmer passwordConfirmer;
        private readonly IAccessTokenIssuer tokenIssuer;
        private readonly IRepository repository;
        private readonly IFileStorageService fileStorageService;
        private readonly ISecurityAlertSender securityAlertSender;
        private readonly ILogger<AccountService> logger;

        public AccountService(UserManager<ApplicationUser> userManager,
                              IPasswordConfirmer passwordConfirmer,
                              IAccessTokenIssuer tokenIssuer,
                              IRepository repository,
                              IFileStorageService fileStorageService,
                              ISecurityAlertSender securityAlertSender,
                              ILogger<AccountService> logger)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.passwordConfirmer = passwordConfirmer;
            this.tokenIssuer = tokenIssuer;
            this.repository = repository;
            this.fileStorageService = fileStorageService;
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

        public async Task DeleteAccountAsync(string userId, DeleteAccountRequest request)
        {
            var user = await userManager.GetByIdAsync(userId);

            // Checked before the password, so a request that would be refused
            // anyway does not use up one of its checks.
            if (await userManager.IsInRoleAsync(user, Admin))
            {
                throw new RequestRefusedException(AdministratorAccountDeletion);
            }

            await passwordConfirmer.ConfirmAsync(user, request.Password);

            await using (var transaction = await repository.BeginTransactionAsync())
            {
                await repository.DeleteRecognitionLogsForUserAsync(user.Id);
                await repository.DetachUserFromAdminAuditLogsAsync(user.Id);
                (await userManager.DeleteAsync(user)).ThrowIfFailed();

                await transaction.CommitAsync();
            }

            // Only once the account is certainly gone, since a rolled back
            // transaction could not bring the files back.
            DeleteUploadedFiles(user.Id);
        }

        // The account is gone by now, so a file that will not go, such as one
        // still being read, must not turn the answer into a failure. It is
        // logged for someone to remove by hand.
        private void DeleteUploadedFiles(string userId)
        {
            try
            {
                fileStorageService.DeleteUserFiles(userId);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "Could not delete the uploaded files of deleted user {UserId}.", userId);
            }
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
            DateRegistered = user.DateRegistered
        };
    }
}
