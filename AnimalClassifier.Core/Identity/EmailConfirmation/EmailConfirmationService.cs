namespace AnimalClassifier.Core.Identity.EmailConfirmation
{
    using AnimalClassifier.Core.Common.Email;
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Settings;
    using AnimalClassifier.Core.Identity.EmailConfirmation.Models;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using static AnimalClassifier.Core.Identity.EmailConfirmation.EmailConfirmationMessages;

    public class EmailConfirmationService : IEmailConfirmationService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly FrontendSettings frontendSettings;
        private readonly ILogger<EmailConfirmationService> logger;

        public EmailConfirmationService(UserManager<ApplicationUser> userManager,
                                        IEmailSender emailSender,
                                        IOptions<FrontendSettings> frontendOptions,
                                        ILogger<EmailConfirmationService> logger)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.frontendSettings = frontendOptions.Value;
            this.logger = logger;
        }

        public async Task SendLinkAsync(ApplicationUser user)
        {
            try
            {
                await SendAsync(user);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not send an email confirmation link to user {UserId}.", user.Id);
            }
        }

        // Unlike at registration, a failure is reported, as sending the link
        // is all the user asked for.
        public async Task ResendLinkAsync(string userId)
        {
            var user = await userManager.GetByIdAsync(userId);

            if (user.EmailConfirmed)
            {
                throw new RequestRefusedException(EmailAlreadyConfirmed);
            }

            await SendAsync(user);
        }

        public async Task ConfirmAsync(ConfirmEmailRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            var token = EmailedLink.DecodeToken(request.Token);

            // Which half of the link does not fit is not told apart, as with a
            // password reset link.
            if (user is null || token is null || !(await userManager.ConfirmEmailAsync(user, token)).Succeeded)
            {
                throw new RequestRefusedException(InvalidEmailConfirmationLink);
            }
        }

        private async Task SendAsync(ApplicationUser user)
        {
            if (user.Email is null)
            {
                return;
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = EmailedLink.Build(frontendSettings, frontendSettings.ConfirmEmailPath, user.Email, token);

            await emailSender.SendAsync(user.Email, EmailConfirmationEmail.Subject, EmailConfirmationEmail.BuildBody(user.FullName, link));
        }
    }
}
