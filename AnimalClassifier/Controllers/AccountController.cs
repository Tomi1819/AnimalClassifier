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
        public Task<IActionResult> GetProfile() =>
            RunAsync(async () => Ok(await accountService.GetProfileAsync(User.Id()!)));

        [HttpPut("name")]
        public Task<IActionResult> ChangeName([FromBody] ChangeNameRequest request) =>
            RunAsync(async () => Ok(await accountService.ChangeNameAsync(User.Id()!, request)));

        /// <summary>
        /// Answers with a new token, because the change ends every session the
        /// account had, the caller's included.
        /// </summary>
        [HttpPost("change-password")]
        public Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request) =>
            RunAsync(async () => Ok(await accountService.ChangePasswordAsync(User.Id()!, request)));

        /// <summary>
        /// Answers with a new token for the same reason: the caller's session
        /// ends along with the others, and carries on with this one.
        /// </summary>
        [HttpPost("sign-out-other-sessions")]
        public Task<IActionResult> SignOutOtherSessions() =>
            RunAsync(async () => Ok(await accountService.SignOutOtherSessionsAsync(User.Id()!)));

        /// <summary>
        /// Answers with a ZIP archive of everything the account holds, for its
        /// owner to keep.
        /// </summary>
        [HttpGet("export")]
        [EnableRateLimiting(DataExportPolicy)]
        public Task<IActionResult> ExportData() =>
            RunAsync(async () => File(await dataExportService.ExportAsync(User.Id()!),
                                      MediaTypeNames.Application.Zip,
                                      ExportFileName()));

        /// <summary>
        /// Nothing is answered, as there is no session left to carry on with.
        /// </summary>
        [HttpDelete]
        public Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request) =>
            RunAsync(async () =>
            {
                await accountService.DeleteAccountAsync(User.Id()!, request);
                return NoContent();
            });

        // Dated, so that copies downloaded on different days sit side by side.
        private static string ExportFileName() => $"animal-classifier-data-{DateTime.UtcNow:yyyy-MM-dd}.zip";

        private static async Task<IActionResult> RunAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (KeyNotFoundException ex)
            {
                return new NotFoundObjectResult(new { message = ex.Message });
            }
            // A refusal, such as a wrong current password, is a bad request
            // rather than a 401, which would tell the frontend that the session
            // itself had ended.
            catch (InvalidOperationException ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }
    }
}
