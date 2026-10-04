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
        /// statistics still count them, but they leave the history and the
        /// search, and their files stay in the owner's copy of their data.
        /// </summary>
        Task ClearHistoryAsync(string userId);
    }
}
