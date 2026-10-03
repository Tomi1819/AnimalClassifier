namespace AnimalClassifier.Core.Identity.Account
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using static AnimalClassifier.Core.Identity.Account.AccountMessages;
    using static AnimalClassifier.Core.Identity.RoleConstants;

    public class AccountDeletionService : IAccountDeletionService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasswordConfirmer passwordConfirmer;
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IAdminAuditLogRepository auditLogs;
        private readonly IUnitOfWork unitOfWork;
        private readonly IFileStorageService fileStorageService;
        private readonly ILogger<AccountDeletionService> logger;

        public AccountDeletionService(UserManager<ApplicationUser> userManager,
                                      IPasswordConfirmer passwordConfirmer,
                                      IRecognitionLogRepository recognitionLogs,
                                      IAdminAuditLogRepository auditLogs,
                                      IUnitOfWork unitOfWork,
                                      IFileStorageService fileStorageService,
                                      ILogger<AccountDeletionService> logger)
        {
            this.userManager = userManager;
            this.passwordConfirmer = passwordConfirmer;
            this.recognitionLogs = recognitionLogs;
            this.auditLogs = auditLogs;
            this.unitOfWork = unitOfWork;
            this.fileStorageService = fileStorageService;
            this.logger = logger;
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

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await recognitionLogs.DeleteAllForUserAsync(user.Id);
                await auditLogs.DetachUserAsync(user.Id);
                (await userManager.DeleteAsync(user)).ThrowIfFailed();
            });

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
    }
}
