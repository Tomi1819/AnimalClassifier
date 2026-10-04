namespace AnimalClassifier.Core.Recognitions.Training
{
    using AnimalClassifier.Core.Recognitions.Training.Models;

    /// <summary>
    /// What the feedback says of the model, for an administrator deciding
    /// what it should learn next.
    /// </summary>
    public interface IFeedbackSummaryService
    {
        Task<FeedbackSummary> GetSummaryAsync(CancellationToken cancellationToken);
    }
}
