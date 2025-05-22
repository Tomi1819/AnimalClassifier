namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    public interface IAnimalService
    {
        Task<List<AnimalSearchResult>> SearchAnimalByNameAsync(string searchTerm);
    }
}
