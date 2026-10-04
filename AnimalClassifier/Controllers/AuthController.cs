namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.EmailConfirmation;
    using AnimalClassifier.Core.Identity.EmailConfirmation.Models;
    using AnimalClassifier.Core.Identity.Passkeys;
    using AnimalClassifier.Core.Identity.Passkeys.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Core.Identity.Passwords.Models;
    using AnimalClassifier.RateLimiting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using static AnimalClassifier.Core.Identity.EmailConfirmation.EmailConfirmationMessages;
    using static AnimalClassifier.Core.Identity.Passwords.PasswordMessages;

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly IPasswordResetService passwordResetService;
        private readonly IPasskeyService passkeyService;
        private readonly IEmailConfirmationService emailConfirmationService;

        public AuthController(IAuthService authService,
                              IPasswordResetService passwordResetService,
                              IPasskeyService passkeyService,
                              IEmailConfirmationService emailConfirmationService)
        {
            this.authService = authService;
            this.passwordResetService = passwordResetService;
            this.passkeyService = passkeyService;
            this.emailConfirmationService = emailConfirmationService;
        }

        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Register)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request) =>
            Ok(await authService.RegisterAsync(request));

        /// <summary>
        /// Anonymous, since the link may be opened on a device the user is
        /// not signed in on.
        /// </summary>
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            await emailConfirmationService.ConfirmAsync(request);

            return Ok(new MessageResponse { Message = EmailConfirmed });
        }

        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
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
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await passwordResetService.ForgotPasswordAsync(request);

            // Deliberately the same answer whether or not the address has an
            // account, so that nobody can use this to learn who is registered.
            return Ok(new MessageResponse { Message = PasswordResetEmailSent });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await passwordResetService.ResetPasswordAsync(request);

            return Ok(new MessageResponse { Message = PasswordReset });
        }
    }
}
