namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
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
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LogInRequest request)
        {
            try
            {
                var response = await authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// The options for signing in with a passkey. Anonymous, and it takes
        /// nothing: no address is named, so this tells a caller nothing about
        /// who has an account or which of them use passkeys.
        /// </summary>
        [HttpPost("passkey/options")]
        public async Task<IActionResult> PasskeyOptions()
        {
            return Ok(await passkeyService.CreateLoginOptionsAsync(HttpContext));
        }

        [HttpPost("passkey/login")]
        public async Task<IActionResult> PasskeyLogin([FromBody] PasskeyCredentialRequest request)
        {
            try
            {
                return Ok(await passkeyService.LoginAsync(request, HttpContext));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting(PasswordResetPolicy)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await passwordResetService.ForgotPasswordAsync(request);

            // Deliberately the same answer whether or not the address has an
            // account, so that nobody can use this to learn who is registered.
            return Ok(new { message = PasswordResetEmailSent });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(PasswordResetPolicy)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await passwordResetService.ResetPasswordAsync(request);
                return Ok(new { message = PasswordChanged });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
