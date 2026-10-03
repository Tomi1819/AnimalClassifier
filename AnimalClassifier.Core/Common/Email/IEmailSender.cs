namespace AnimalClassifier.Core.Common.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string recipient, string subject, string htmlBody);
    }
}
