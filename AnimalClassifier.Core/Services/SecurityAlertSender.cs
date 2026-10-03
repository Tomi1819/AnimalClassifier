namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Common.Email;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.Extensions.Logging;
    using static Helpers.SecurityAlertEmail;

    public class SecurityAlertSender : ISecurityAlertSender
    {
        private readonly IEmailSender emailSender;
        private readonly ILogger<SecurityAlertSender> logger;

        public SecurityAlertSender(IEmailSender emailSender, ILogger<SecurityAlertSender> logger)
        {
            this.emailSender = emailSender;
            this.logger = logger;
        }

        public Task PasswordChangedAsync(ApplicationUser user) =>
            SendAsync(user, PasswordChangedSubject, BuildPasswordChangedBody(user.FullName));

        public Task PasskeyAddedAsync(ApplicationUser user, string passkeyName) =>
            SendAsync(user, PasskeyAddedSubject, BuildPasskeyAddedBody(user.FullName, passkeyName));

        public Task PasskeyRemovedAsync(ApplicationUser user, string passkeyName) =>
            SendAsync(user, PasskeyRemovedSubject, BuildPasskeyRemovedBody(user.FullName, passkeyName));

        public Task OtherSessionsSignedOutAsync(ApplicationUser user) =>
            SendAsync(user, OtherSessionsSignedOutSubject, BuildOtherSessionsSignedOutBody(user.FullName));

        private async Task SendAsync(ApplicationUser user, string subject, string htmlBody)
        {
            if (user.Email is null)
            {
                return;
            }

            try
            {
                await emailSender.SendAsync(user.Email, subject, htmlBody);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not send the security alert \"{Subject}\" to user {UserId}.",
                    subject, user.Id);
            }
        }
    }
}
