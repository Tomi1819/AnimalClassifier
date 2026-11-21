namespace AnimalClassifier.Core.Recognitions.History
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Recognitions.History.Models;

    /// <summary>
    /// A user's own recognitions, as their history lists them.
    /// </summary>
    public interface IRecognitionHistoryService
    {
        /// <summary>
        /// One page of one user's recognitions, most recent first, leaving out
        /// any they have cleared.
        /// </summary>
        /// <param name="page">Which page, counted from 1.</param>
        Task<PagedResult<RecognitionHistoryItem>> GetHistoryAsync(string userId, int page, CancellationToken cancellationToken);

        /// <summary>
        /// Clears one user's history. The recognitions are kept, so the
        /// statistics still count them, but they leave the history and the
        /// search, and their files stay in the owner's copy of their data.
        /// </summary>
        Task ClearHistoryAsync(string userId);

        /// <summary>
        /// Deletes one of a user's recognitions for good, whether or not it was
        /// cleared: its file, the feedback given on it, and with them its place
        /// in the statistics and the collection.
        /// </summary>
        /// <exception cref="NotFoundException">
        /// When the user has no such recognition, which is also the answer for
        /// another user's.
        /// </exception>
        Task DeleteRecognitionAsync(string userId, int recognitionId);
    }
}
