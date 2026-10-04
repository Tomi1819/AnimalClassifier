namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Common.Exceptions;

    /// <summary>
    /// Caps how many uploads are worked on at once, whoever sends them. Each
    /// takes a processor for as long as the model runs on it, and a video for
    /// a run per frame, so beyond a few at a time they only slow each other
    /// and everything else the server answers. The rest wait their turn, up
    /// to a point.
    /// </summary>
    public interface IClassificationLimiter
    {
        /// <summary>
        /// Waits for a turn, which lasts until what is returned is disposed.
        /// </summary>
        /// <exception cref="ServiceBusyException">
        /// When too many are waiting already.
        /// </exception>
        Task<IDisposable> WaitTurnAsync(CancellationToken cancellationToken);
    }
}
