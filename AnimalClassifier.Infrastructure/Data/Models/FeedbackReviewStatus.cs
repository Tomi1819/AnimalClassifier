namespace AnimalClassifier.Infrastructure.Data.Models
{
    /// <summary>
    /// Where an administrator's review of a feedback stands. Only feedback
    /// whose user allowed training is reviewed, and only an accepted one is
    /// trained on.
    /// </summary>
    public enum FeedbackReviewStatus
    {
        Pending,
        Accepted,
        Rejected
    }
}
