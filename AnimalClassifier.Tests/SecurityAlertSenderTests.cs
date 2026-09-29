namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.Services;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.Extensions.Logging.Abstractions;

    public class SecurityAlertSenderTests
    {
        // The change the alert is about has already been made, so a mail
        // server that is down must not turn it into an error.
        [Fact]
        public async Task AnAlertTheServerFailsToSend_DoesNotFail()
        {
            var sender = new SecurityAlertSender(new FailingEmailSender(), NullLogger<SecurityAlertSender>.Instance);

            var exception = await Record.ExceptionAsync(() => sender.PasswordChangedAsync(new ApplicationUser
            {
                FullName = "Test User",
                Email = "test@example.test"
            }));

            Assert.Null(exception);
        }

        // An alert that reads the user's own text back to them must not let
        // that text write the email.
        [Fact]
        public async Task ThePasskeyName_IsEncoded()
        {
            var emails = new RecordingEmailSender();
            var sender = new SecurityAlertSender(emails, NullLogger<SecurityAlertSender>.Instance);
            var user = new ApplicationUser { FullName = "Test User", Email = "test@example.test" };

            await sender.PasskeyAddedAsync(user, "<b>Laptop</b>");

            var body = Assert.Single(emails.BodiesSentTo(user.Email));
            Assert.DoesNotContain("<b>Laptop</b>", body);
            Assert.Contains("&lt;b&gt;Laptop&lt;/b&gt;", body);
        }

        private class FailingEmailSender : IEmailSender
        {
            public Task SendAsync(string recipient, string subject, string htmlBody) =>
                throw new InvalidOperationException("The mail server is down.");
        }
    }
}
