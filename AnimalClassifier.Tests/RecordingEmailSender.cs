namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Contracts;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Keeps the messages instead of sending them, so that a test can read what
    /// the user would have received. Tests share one instance and run in
    /// parallel, so each looks itself up by the address it registered.
    /// </summary>
    public class RecordingEmailSender : IEmailSender
    {
        private static readonly Regex LinkPattern = new("href=\"([^\"]+)\"", RegexOptions.Compiled);

        private readonly ConcurrentQueue<(string Recipient, string Subject, string Body)> messages = new();

        public Task SendAsync(string recipient, string subject, string htmlBody)
        {
            messages.Enqueue((recipient, subject, htmlBody));

            return Task.CompletedTask;
        }

        public bool AnySentTo(string recipient) =>
            messages.Any(message => message.Recipient == recipient);

        public string? LinkSentTo(string recipient)
        {
            var body = messages.Where(message => message.Recipient == recipient)
                               .Select(message => message.Body)
                               .LastOrDefault();

            if (body is null)
            {
                return null;
            }

            var match = LinkPattern.Match(body);

            // The body is HTML, where the ampersands separating query
            // parameters are written as entities.
            return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
        }
    }
}
