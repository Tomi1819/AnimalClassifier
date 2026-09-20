namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Writes messages to the log instead of sending them, so that working on
    /// the app needs no SMTP server. A password reset link mailed this way is
    /// readable by anyone who can read the log, which is why nothing registers
    /// this outside development.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            this.logger = logger;
        }

        public Task SendAsync(string recipient, string subject, string htmlBody)
        {
            logger.LogInformation("Email to {Recipient}, \"{Subject}\":\n{Body}",
                recipient, subject, htmlBody);

            return Task.CompletedTask;
        }
    }
}
