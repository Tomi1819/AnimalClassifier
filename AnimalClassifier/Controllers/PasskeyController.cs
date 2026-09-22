namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// A user's own passkeys. Registering one is a change to the account that
    /// owns it, so all of this is for a caller who is already signed in;
    /// signing in with a passkey lives on the auth controller.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PasskeyController : ControllerBase
    {
        private readonly IPasskeyService passkeyService;

        public PasskeyController(IPasskeyService passkeyService)
        {
            this.passkeyService = passkeyService;
        }

        [HttpGet]
        public Task<IActionResult> GetPasskeys() =>
            RunAsync(async () => Ok(await passkeyService.GetPasskeysAsync(User.Id()!)));

        [HttpPost("options")]
        public Task<IActionResult> CreateOptions() =>
            RunAsync(async () => Ok(await passkeyService.CreateRegistrationOptionsAsync(User.Id()!, HttpContext)));

        [HttpPost]
        public Task<IActionResult> Register([FromBody] PasskeyRegistrationRequest request) =>
            RunAsync(async () => Ok(await passkeyService.RegisterAsync(User.Id()!, request, HttpContext)));

        [HttpDelete("{id}")]
        public Task<IActionResult> Remove(string id) =>
            RunAsync(async () =>
            {
                await passkeyService.RemoveAsync(User.Id()!, id);
                return NoContent();
            });

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
            catch (InvalidOperationException ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }
    }
}
