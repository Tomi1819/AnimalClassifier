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
                .Select(g => new AnimalSearchResult
                {
                    AnimalName = g.Key,
                    Count = g.Count(),
                    ImagePaths = g
                    .Select(x => x.ImagePath)
                    .Distinct()
                    .ToList()
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            return filtered;
        }
    }
}
