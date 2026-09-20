namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Services.Helpers;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Text;
    using static Constants.SecurityConstants;

    public class PasswordResetService : IPasswordResetService
    {
        private const string EmailParameter = "email";
        private const string TokenParameter = "token";

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly FrontendSettings frontendSettings;
        private readonly ILogger<PasswordResetService> logger;

        public PasswordResetService(UserManager<ApplicationUser> userManager,
                                    IEmailSender emailSender,
                                    IOptions<FrontendSettings> frontendOptions,
                                    ILogger<PasswordResetService> logger)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
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
            var link = BuildResetLink(user.Email, token);

            try
            {
                await emailSender.SendAsync(
                    user.Email,
                    PasswordResetEmail.Subject,
                    PasswordResetEmail.BuildBody(user.FullName, link, PasswordResetTokenLifespan));
            }
            catch (Exception exception)
            {
                // The caller hears nothing about this. An error here, where an
                // unregistered address gets a cheerful 200, would answer the
                // question the endpoint refuses to answer.
                logger.LogError(exception, "Could not send a password reset email to user {UserId}.", user.Id);
            }
        }

        /// <summary>
        /// The token travels in a query string, and the form Identity hands it
        /// over in contains characters that would not survive the journey.
        /// </summary>
        private string BuildResetLink(string email, string token)
        {
            var query = QueryString.Create(new Dictionary<string, string?>
            {
                [EmailParameter] = email,
                [TokenParameter] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
            });

            return $"{frontendSettings.BaseUrl.TrimEnd('/')}{frontendSettings.ResetPasswordPath}{query}";
        }
    }
}
