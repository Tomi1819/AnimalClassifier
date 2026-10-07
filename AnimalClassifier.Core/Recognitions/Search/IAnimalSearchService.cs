namespace AnimalClassifier.Core.Recognitions.Search
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Search.Models;

    /// <summary>
    /// Finding animals by name among everything recognised in an image, by
    /// every user. A recognition cleared from its owner's history is left
    /// out, since its owner put it away.
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
        /// When no animal recognised in an image matches it.
        /// </exception>
        Task<IReadOnlyList<AnimalSearchResult>> SearchAsync(string? searchTerm, CancellationToken cancellationToken);
    }
}
