namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Writes messages to the log instead of sending them, so that working on
    /// the app needs neither an SMTP server nor credentials. Whatever is mailed
    /// this way, a password reset link included, is then readable by anyone who
    /// can read the log, which is why this is registered in development only.
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
            logger.LogInformation("Email to {Recipient}, \"{Subject}\":{NewLine}{Body}",
                recipient, subject, Environment.NewLine, htmlBody);

            return Task.CompletedTask;
        }
    }
}
