namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Common;

    public class AnimalService : IAnimalService
    {
        private readonly IRepository repository;
        private readonly IFileValidator fileValidator;

        public AnimalService(IRepository repository, IFileValidator fileValidator)
        {
            this.repository = repository;
            this.fileValidator = fileValidator;
        }
        public async Task<List<AnimalSearchResult>> SearchAnimalByNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<AnimalSearchResult>();

            var logs = await repository.GetAllRecognitionLogsAsync();

            var filtered = logs
                .Where(l => l.AnimalName.Contains(searchTerm))
                .Where(l => fileValidator.IsImage(l.ImagePath))
                .GroupBy(l => l.AnimalName)
                .Select(g => new
                {
                    AnimalName = g.Key,
                    Count = g.Count(),
                    ImagePaths = g
                    .Select(x => x.ImagePath)
                    .Distinct()
                    .ToList()
                })
                .ToList();

            int maxCount = filtered.Max(f => f.Count);

            var results = filtered
                .Select(r => new AnimalSearchResult
                {
                    AnimalName = r.AnimalName,
                    Count = r.Count,
                    ImagePaths = r.ImagePaths,
                    Accuracy = maxCount > 0 ? (float)r.Count / maxCount : 0
                })
                .Where(r => r.Accuracy >= 0.7)
                .OrderByDescending(r => r.Accuracy)
                .ToList();

            return results;
        }
    }
}
