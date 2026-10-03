namespace AnimalClassifier.Core.Recognitions.History
{
    using AnimalClassifier.Core.Recognitions.History.Models;

    /// <summary>
    /// A user's own recognitions, as their history lists them.
    /// </summary>
    public interface IRecognitionHistoryService
    {
        /// <summary>
        /// One user's recognitions, most recent first, leaving out any they
        /// have cleared.
        /// </summary>
        Task<IReadOnlyList<RecognitionHistoryItem>> GetHistoryAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Clears one user's history. The recognitions are kept, so the
        /// statistics and search pages still count them; they are only hidden
        /// from their owner's history.
        /// </summary>
        Task ClearHistoryAsync(string userId);
    }
}
