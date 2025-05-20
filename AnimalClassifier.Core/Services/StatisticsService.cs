namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Common;

    public class StatisticsService : IStatisticsService
    {
        private readonly IRepository repository;

        public StatisticsService(IRepository repository)
        {
            this.repository = repository;
        }

        public async Task<List<MostCommonAnimal>> GetMostCommonAnimalAsync()
        {
            var logs = await repository.GetAllRecognitionLogsAsync();

            var mostCommon = logs
                .GroupBy(l => l.AnimalName)
                .Select(g => new MostCommonAnimal
                {
                    AnimalName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(a => a.Count)
                .Take(3)
                .ToList();

            if (mostCommon is null)
            {
                return null!;
            }

            return mostCommon;
        }

        public async Task<int> GetTotalClassificationAsync()
        {
            var logs = await repository.GetAllRecognitionLogsAsync();
            return logs.Count();
        }

        public async Task<int> GetUniqueUserCountAsync()
        {
            var logs = await repository.GetAllRecognitionLogsAsync();
            return logs.Select(u => u.UserId)
                .Distinct()
                .Count();
        }
    }
}
