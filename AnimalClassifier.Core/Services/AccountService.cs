namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Extensions;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using static Constants.MessageConstants;
    using static Constants.RoleConstants;
    using static Constants.ValidationConstants;

    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IAccessTokenIssuer tokenIssuer;
        private readonly IRepository repository;
        private readonly IFileStorageService fileStorageService;
        private readonly ISecurityAlertSender securityAlertSender;
        private readonly ILogger<AccountService> logger;

        public AccountService(UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              IAccessTokenIssuer tokenIssuer,
                              IRepository repository,
                              IFileStorageService fileStorageService,
                              ISecurityAlertSender securityAlertSender,
                              ILogger<AccountService> logger)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.tokenIssuer = tokenIssuer;
            this.repository = repository;
            this.fileStorageService = fileStorageService;
            this.securityAlertSender = securityAlertSender;
        }

        public async Task<AccountProfile> GetProfileAsync(string userId) =>
            ToProfile(await FindUserAsync(userId));

        public async Task<AccountProfile> ChangeNameAsync(string userId, ChangeNameRequest request)
        {
            var user = await FindUserAsync(userId);

            user.FullName = TidyFullName(request.FullName);
            (await userManager.UpdateAsync(user)).ThrowIfFailed();

            return ToProfile(user);
        }

        public async Task<LoginResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await FindUserAsync(userId);

            await ConfirmPasswordAsync(user, request.CurrentPassword);

            (await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)).ThrowIfFailed();
            await securityAlertSender.PasswordChangedAsync(user);

            return await tokenIssuer.IssueAsync(user);
        }

        public async Task<LoginResponse> SignOutOtherSessionsAsync(string userId)
        {
            var user = await FindUserAsync(userId);

            (await userManager.UpdateSecurityStampAsync(user)).ThrowIfFailed();
            await securityAlertSender.OtherSessionsSignedOutAsync(user);

            return await tokenIssuer.IssueAsync(user);
        }

        public async Task DeleteAccountAsync(string userId, DeleteAccountRequest request)
        {
            var user = await FindUserAsync(userId);

            // Checked before the password, so a request that would be refused
            // anyway does not count towards a lockout.
            if (await userManager.IsInRoleAsync(user, Admin))
            {
                throw new InvalidOperationException(AdministratorAccountDeletion);
            }

            await ConfirmPasswordAsync(user, request.Password);

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

        // Unlike at registration, the letters are left as typed; only the
        // spacing around and between the words is tidied.
        private static string TidyFullName(string fullName)
        {
            var words = fullName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var tidied = string.Join(Space, words);

            if (tidied.Length == 0)
            {
                throw new InvalidOperationException(EmptyFullName);
            }

            if (tidied.Length > FullNameMaxLength)
            {
                throw new InvalidOperationException(string.Format(FullNameTooLong, FullNameMaxLength));
            }

            return tidied;
        }

        private static AccountProfile ToProfile(ApplicationUser user) => new()
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            DateRegistered = user.DateRegistered
        };
    }
}
