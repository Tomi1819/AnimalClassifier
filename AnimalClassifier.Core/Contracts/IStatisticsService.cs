namespace AnimalClassifier.Core.Contracts
{
    using DTO;
    public interface IStatisticsService
    {
        Task<int> GetTotalClassificationAsync();
        Task<int> GetUniqueUserCountAsync();
        Task<MostCommonAnimal> GetMostCommonAnimalAsync();
    }
}
