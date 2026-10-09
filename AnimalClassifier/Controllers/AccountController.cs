namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Identity.Account;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.EmailConfirmation;
    using AnimalClassifier.Extensions;
    using AnimalClassifier.RateLimiting;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using System.Net.Mime;
    using static AnimalClassifier.Core.Identity.EmailConfirmation.EmailConfirmationMessages;

    /// <summary>
    /// A signed-in user's own account: what it holds, and the changes they make
    /// to it. Resetting a forgotten password is for someone who cannot sign in,
    /// so it lives on the auth controller instead.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;
        private readonly IDataExportService dataExportService;
        private readonly IAccountDeletionService accountDeletionService;
        private readonly IEmailConfirmationService emailConfirmationService;
        private readonly TimeProvider timeProvider;

        public AccountController(IAccountService accountService,
                                 IDataExportService dataExportService,
                                 IAccountDeletionService accountDeletionService,
                                 IEmailConfirmationService emailConfirmationService,
                                 TimeProvider timeProvider)
        {
            this.accountService = accountService;
            this.dataExportService = dataExportService;
            this.accountDeletionService = accountDeletionService;
            this.emailConfirmationService = emailConfirmationService;
            this.timeProvider = timeProvider;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile() =>
            Ok(await accountService.GetProfileAsync(User.RequiredId()));

        [HttpPost("resend-confirmation-email")]
        [EnableRateLimiting(RateLimitPolicies.ConfirmationEmail)]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            await emailConfirmationService.ResendLinkAsync(User.RequiredId());

            return Ok(new MessageResponse { Message = EmailConfirmationLinkSent });
        }

        [HttpPut("name")]
        public async Task<IActionResult> ChangeName([FromBody] ChangeNameRequest request) =>
            Ok(await accountService.ChangeNameAsync(User.RequiredId(), request));

        /// <summary>
        /// Answers with a new token, because the change ends every session the
        /// account had, the caller's included.
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request) =>
            Ok(await accountService.ChangePasswordAsync(User.RequiredId(), request));

        /// <summary>
        /// Answers with a new token for the same reason: the caller's session
        /// ends along with the others, and carries on with this one. It runs
        /// out when the caller's old token would have.
        /// </summary>
        [HttpPost("sign-out-other-sessions")]
        public async Task<IActionResult> SignOutOtherSessions() =>
            Ok(await accountService.SignOutOtherSessionsAsync(User.RequiredId(), User.TokenExpiration()));

        /// <summary>
        /// Answers with a ZIP archive of everything the account holds, for its
        /// owner to keep.
        /// </summary>
        [HttpGet("export")]
        [EnableRateLimiting(RateLimitPolicies.DataExport)]
        public async Task<IActionResult> ExportData() =>
            File(await dataExportService.ExportAsync(User.RequiredId()), MediaTypeNames.Application.Zip, ExportFileName());

        /// <summary>
        /// Nothing is answered, as there is no session left to carry on with.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            await accountDeletionService.DeleteAccountAsync(User.RequiredId(), request);

            return NoContent();
        }

        // Dated, so that copies downloaded on different days sit side by side.
        private string ExportFileName() => $"animal-classifier-data-{timeProvider.GetUtcNow():yyyy-MM-dd}.zip";
    }
}
