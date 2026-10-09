namespace AnimalClassifier.Infrastructure.Email
{
    using AnimalClassifier.Core.Common.Email;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Writes messages to the log instead of sending them, so that working on
    /// the app needs no SMTP server. A password reset link mailed this way is
    /// readable by anyone who can read the log, which is why nothing registers
    /// this outside development.
    /// </summary>
    public partial class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            this.logger = logger;
        }

        public Task SendAsync(string recipient, string subject, string htmlBody)
        {
            LogEmail(recipient, subject, htmlBody);

            return Task.CompletedTask;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Email to {Recipient}, \"{Subject}\":\n{Body}")]
        private partial void LogEmail(string recipient, string subject, string body);
    }
}
