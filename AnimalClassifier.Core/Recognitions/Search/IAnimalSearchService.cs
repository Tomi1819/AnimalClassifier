namespace AnimalClassifier.Core.Recognitions.Search
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Search.Models;

    /// <summary>
    /// Finding animals by name among everything recognised in one user's own
    /// images. No other user's images are ever found, and a recognition
    /// cleared from the history is left out, since its owner put it away.
    /// </summary>
    public interface IAnimalSearchService
    {
        /// <summary>
        /// The animals whose name contains the term, whatever its case, that
        /// were recognised nearly as often as the one recognised most; see
        /// <see cref="AnimalSearchService.MinAccuracy"/>. Most recognised
        /// first, each with its most recent images; see
        /// <see cref="AnimalSearchService.MaxImagesPerAnimal"/>.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the term is blank.
        /// </exception>
        /// <exception cref="NotFoundException">
        /// When no animal recognised in one of the user's images matches it.
        /// </exception>
        Task<IReadOnlyList<AnimalSearchResult>> SearchAsync(string userId, string? searchTerm, CancellationToken cancellationToken);
    }
}
