namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Identity.Passkeys;
    using AnimalClassifier.Core.Identity.Passkeys.Models;
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
        public async Task<IActionResult> GetPasskeys() =>
            Ok(await passkeyService.GetPasskeysAsync(User.RequiredId()));

        /// <summary>
        /// Takes the account's password, since a passkey outlasts the session
        /// that adds it.
        /// </summary>
        [HttpPost("options")]
        public async Task<IActionResult> CreateOptions([FromBody] PasskeyRegistrationOptionsRequest request) =>
            Ok(await passkeyService.CreateRegistrationOptionsAsync(User.RequiredId(), request));

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] PasskeyRegistrationRequest request) =>
            Ok(await passkeyService.RegisterAsync(User.RequiredId(), request));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(string id)
        {
            await passkeyService.RemoveAsync(User.RequiredId(), id);

            return NoContent();
        }
    }
}
