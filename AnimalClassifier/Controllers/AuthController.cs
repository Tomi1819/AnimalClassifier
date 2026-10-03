namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Core.Identity.Passwords.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using static Constants.MessageConstants;
    using static Core.Constants.ConfigConstants;

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly IPasswordResetService passwordResetService;
        private readonly IPasskeyService passkeyService;

        public AuthController(IAuthService authService,
                              IPasswordResetService passwordResetService,
                              IPasskeyService passkeyService)
        {
            this.authService = authService;
            this.passwordResetService = passwordResetService;
            this.passkeyService = passkeyService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request) =>
            Ok(await authService.RegisterAsync(request));

        [HttpPost("login")]
        [EnableRateLimiting(LoginPolicy)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request) =>
            Ok(await authService.LoginAsync(request));

        /// <summary>
        /// The options for signing in with a passkey. Anonymous, and it takes
        /// nothing: no address is named, so this tells a caller nothing about
        /// who has an account or which of them use passkeys.
        /// </summary>
        [HttpPost("passkey/options")]
        public async Task<IActionResult> PasskeyOptions() =>
            Ok(await passkeyService.CreateLoginOptionsAsync(HttpContext));

        [HttpPost("passkey/login")]
        public async Task<IActionResult> PasskeyLogin([FromBody] PasskeyCredentialRequest request) =>
            Ok(await passkeyService.LoginAsync(request, HttpContext));

        [HttpPost("forgot-password")]
        [EnableRateLimiting(PasswordResetPolicy)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await passwordResetService.ForgotPasswordAsync(request);

            // Deliberately the same answer whether or not the address has an
            // account, so that nobody can use this to learn who is registered.
            return Ok(new MessageResponse { Message = PasswordResetEmailSent });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(PasswordResetPolicy)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await passwordResetService.ResetPasswordAsync(request);

            return Ok(new MessageResponse { Message = PasswordChanged });
        }
    }
}
