namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Options;
    using MimeKit;

    public class SmtpEmailSender : IEmailSender
    {
        // The port that expects TLS from the first byte, rather than the
        // upgrade mid-conversation that every other port uses.
        private const int ImplicitTlsPort = 465;

        /// <summary>
        /// A caller waits out this whole conversation, so a server that has
        /// stopped answering must not hold the request for MailKit's own two
        /// minutes.
        /// </summary>
        private const int TimeoutMilliseconds = 15_000;

        private readonly EmailSettings settings;

        public SmtpEmailSender(IOptions<EmailSettings> emailOptions)
        {
            this.settings = emailOptions.Value;
        }

        public async Task SendAsync(string recipient, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient { Timeout = TimeoutMilliseconds };

            await client.ConnectAsync(settings.Host, settings.Port, SocketOptionsFor(settings.Port));

            // A server that wants no credentials refuses the attempt to
            // authenticate, so only offer them when there are some to offer.
            if (!string.IsNullOrWhiteSpace(settings.UserName))
            {
                await client.AuthenticateAsync(settings.UserName, settings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
        }

        // Anywhere but the implicit TLS port, the connection starts in the clear
        // and is upgraded if the server offers STARTTLS.
        private static SecureSocketOptions SocketOptionsFor(int port) =>
            port == ImplicitTlsPort
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;
    }
}
