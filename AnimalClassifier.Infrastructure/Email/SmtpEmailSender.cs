namespace AnimalClassifier.Infrastructure.Email
{
    using AnimalClassifier.Core.Common.Email;
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Options;
    using MimeKit;
    using System.Net;

    public class SmtpEmailSender : IEmailSender
    {
        // The port that expects TLS from the first byte, rather than the
        // upgrade mid-conversation that every other port uses.
        private const int ImplicitTlsPort = 465;

        private const string LocalHost = "localhost";

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

            await client.ConnectAsync(settings.Host, settings.Port, SocketOptionsFor(settings.Host, settings.Port));

            // A server that wants no credentials refuses the attempt to
            // authenticate, so only offer them when there are some to offer.
            if (!string.IsNullOrWhiteSpace(settings.UserName))
            {
                await client.AuthenticateAsync(settings.UserName, settings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
        }

        /// <summary>
        /// Anywhere but the implicit TLS port, the connection starts in the
        /// clear and has to be upgraded with STARTTLS, or the credentials and
        /// the reset links would cross the network readable. Only a server on
        /// this machine, such as a mail catcher in development, may go without.
        /// </summary>
        public static SecureSocketOptions SocketOptionsFor(string host, int port)
        {
            if (port == ImplicitTlsPort)
            {
                return SecureSocketOptions.SslOnConnect;
            }

            return IsLoopback(host) ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.StartTls;
        }

        private static bool IsLoopback(string host) =>
            string.Equals(host, LocalHost, StringComparison.OrdinalIgnoreCase)
            || (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
    }
}
