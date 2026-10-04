namespace AnimalClassifier.Tests.Common
{
    using AnimalClassifier.Core.Common.Email;
    using MailKit.Security;

    public class SmtpEmailSenderTests
    {
        public static TheoryData<string, int, SecureSocketOptions> SocketOptions => new()
        {
            { "smtp.example.test", 465, SecureSocketOptions.SslOnConnect },

            // A server that will not upgrade the connection is refused, rather
            // than sent the credentials and the links in the clear.
            { "smtp.example.test", 587, SecureSocketOptions.StartTls },

            // A mail catcher on this machine, which nothing crosses a network to reach.
            { "localhost", 1025, SecureSocketOptions.StartTlsWhenAvailable },
            { "127.0.0.1", 1025, SecureSocketOptions.StartTlsWhenAvailable },
            { "::1", 1025, SecureSocketOptions.StartTlsWhenAvailable }
        };

        [Theory]
        [MemberData(nameof(SocketOptions))]
        public void TheConnection_IsEncrypted_UnlessTheServerIsOnThisMachine(string host, int port, SecureSocketOptions expected)
        {
            Assert.Equal(expected, SmtpEmailSender.SocketOptionsFor(host, port));
        }
    }
}
