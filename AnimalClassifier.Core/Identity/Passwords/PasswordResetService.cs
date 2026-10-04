namespace AnimalClassifier.Core.Identity.Passwords
{
    using AnimalClassifier.Core.Common.Email;
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Settings;
    using AnimalClassifier.Core.Identity.Passwords.Models;
    using AnimalClassifier.Core.Identity.SecurityAlerts;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using static AnimalClassifier.Core.Identity.Passwords.PasswordMessages;

    public class PasswordResetService : IPasswordResetService
    {
        private static readonly string InvalidTokenCode = new IdentityErrorDescriber().InvalidToken().Code;

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly ISecurityAlertSender securityAlertSender;
        private readonly FrontendSettings frontendSettings;
        private readonly ILogger<PasswordResetService> logger;

        public PasswordResetService(UserManager<ApplicationUser> userManager,
                                    IEmailSender emailSender,
                                    ISecurityAlertSender securityAlertSender,
                                    IOptions<FrontendSettings> frontendOptions,
                                    ILogger<PasswordResetService> logger)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.securityAlertSender = securityAlertSender;
            this.frontendSettings = frontendOptions.Value;
            this.logger = logger;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user?.Email is null)
            {
                return;
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = EmailedLink.Build(frontendSettings, frontendSettings.ResetPasswordPath, user.Email, token);

            try
            {
                await emailSender.SendAsync(
                    user.Email,
                    PasswordResetEmail.Subject,
                    PasswordResetEmail.BuildBody(user.FullName, link));
            }
            catch (Exception exception)
            {
                // The caller hears nothing about this. An error here, where an
                // unregistered address gets a cheerful 200, would answer the
                // question the endpoint refuses to answer.
                logger.LogError(exception, "Could not send a password reset email to user {UserId}.", user.Id);
            }
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            var token = EmailedLink.DecodeToken(request.Token);

            if (user is null || token is null)
            {
                // Told apart from a bad token by nothing at all. The link is
                // everything the caller has, and which half of it does not fit
                // is not a thing they need to be told.
                throw new RequestRefusedException(InvalidPasswordResetLink);
            }

            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (result.Errors.Any(error => error.Code == InvalidTokenCode))
            {
                throw new RequestRefusedException(InvalidPasswordResetLink);
            }

            // Anything else is the new password failing the rules, which the
            // user can do something about once they are told what went wrong.
            result.ThrowIfFailed();

            await securityAlertSender.PasswordChangedAsync(user);
        }
    }
}
