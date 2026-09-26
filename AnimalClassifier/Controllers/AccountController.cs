namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// A signed-in user's changes to their own account. Resetting a forgotten
    /// password is for someone who cannot sign in, so it lives on the auth
    /// controller instead.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;

        public AccountController(IAccountService accountService)
        {
            this.accountService = accountService;
        }

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
