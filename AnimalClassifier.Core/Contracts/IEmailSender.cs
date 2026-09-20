namespace AnimalClassifier.Core.Contracts
{
    public interface IEmailSender
    {
        Task SendAsync(string recipient, string subject, string htmlBody);
    }
}
