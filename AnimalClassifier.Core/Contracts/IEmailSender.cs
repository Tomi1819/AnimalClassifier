namespace AnimalClassifier.Core.Contracts
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends one message, returning once the server has accepted it for
        /// delivery. Acceptance is not delivery: a message can still bounce
        /// afterwards without the caller ever hearing about it.
        /// </summary>
        Task SendAsync(string recipient, string subject, string htmlBody);
    }
}
