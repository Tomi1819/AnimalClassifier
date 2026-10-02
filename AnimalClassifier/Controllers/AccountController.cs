namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using System.Net.Mime;
    using static Core.Constants.ConfigConstants;

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

        public AccountController(IAccountService accountService, IDataExportService dataExportService)
        {
            this.accountService = accountService;
            this.dataExportService = dataExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile() =>
            Ok(await accountService.GetProfileAsync(User.RequiredId()));

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
        [EnableRateLimiting(DataExportPolicy)]
        public async Task<IActionResult> ExportData() =>
            File(await dataExportService.ExportAsync(User.RequiredId()), MediaTypeNames.Application.Zip, ExportFileName());

        /// <summary>
        /// Nothing is answered, as there is no session left to carry on with.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            await accountService.DeleteAccountAsync(User.RequiredId(), request);

            return NoContent();
        }

        // Dated, so that copies downloaded on different days sit side by side.
        private static string ExportFileName() => $"animal-classifier-data-{DateTime.UtcNow:yyyy-MM-dd}.zip";
    }
}
