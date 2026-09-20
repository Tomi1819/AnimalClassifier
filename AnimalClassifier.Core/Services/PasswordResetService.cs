namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Extensions;
    using AnimalClassifier.Core.Services.Helpers;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Text;
    using static Constants.MessageConstants;
    using static Constants.SecurityConstants;

    public class PasswordResetService : IPasswordResetService
    {
        private const string EmailParameter = "email";
        private const string TokenParameter = "token";

        private static readonly string InvalidTokenCode = new IdentityErrorDescriber().InvalidToken().Code;

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

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null)
            {
                // Told apart from a bad token by nothing at all. The link is
                // everything the caller has, and which half of it does not fit
                // is not a thing they need to be told.
                throw new InvalidOperationException(InvalidPasswordResetLink);
            }

            var result = await userManager.ResetPasswordAsync(user, DecodeToken(request.Token), request.NewPassword);

            if (result.Errors.Any(error => error.Code == InvalidTokenCode))
            {
                throw new InvalidOperationException(InvalidPasswordResetLink);
            }

            // Anything else is the new password failing the rules, which the
            // user can do something about once they are told what went wrong.
            result.ThrowIfFailed();
        }

        // The token travels in a query string, and the form Identity hands it
        // over in contains characters that would not survive the journey.
        private string BuildResetLink(string email, string token)
        {
            var query = QueryString.Create(new Dictionary<string, string?>
            {
                [EmailParameter] = email,
                [TokenParameter] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
            });

            return $"{frontendSettings.BaseUrl.TrimEnd('/')}{frontendSettings.ResetPasswordPath}{query}";
        }

        private static string DecodeToken(string token)
        {
            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                // A link mangled on its way here is no longer a link, and
                // Identity should never see what is left of it.
                throw new InvalidOperationException(InvalidPasswordResetLink);
            }
        }
    }
}
