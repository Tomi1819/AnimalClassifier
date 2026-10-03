namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data.Repositories;

    public class AnimalService : IAnimalService
    {
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IFileValidator fileValidator;

        public AnimalService(IRecognitionLogRepository recognitionLogs, IFileValidator fileValidator)
        {
            this.recognitionLogs = recognitionLogs;
            this.fileValidator = fileValidator;
        }
        public async Task<List<AnimalSearchResult>> SearchAnimalByNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<AnimalSearchResult>();

            var logs = await recognitionLogs.GetAllAsync();

            var filtered = logs
                .Where(l => l.AnimalName.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase))
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

            if (filtered.Count == 0)
                return new List<AnimalSearchResult>();

            int maxCount = filtered.Max(f => f.Count);

            var results = filtered
                .Select(r => new AnimalSearchResult
                {
                    AnimalName = r.AnimalName,
                    Count = r.Count,
                    ImagePaths = r.ImagePaths,
                    Accuracy = (float)r.Count / maxCount
                })
                .Where(r => r.Accuracy >= 0.7)
                .OrderByDescending(r => r.Accuracy)
                .ToList();

            return results;
        }
    }
}
