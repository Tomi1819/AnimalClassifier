namespace AnimalClassifier.Core.Recognitions.Search
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Core.Recognitions.Search.Models;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using static AnimalClassifier.Core.Recognitions.Search.SearchMessages;

    public class AnimalSearchService : IAnimalSearchService
    {
        /// <summary>
        /// How often, next to the match recognised most, an animal has to have
        /// been recognised to be shown. A term such as "a" matches nearly
        /// every animal, and this keeps the rare ones from crowding the page.
        /// </summary>
        public const float MinAccuracy = 0.7f;

        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IMediaLinkService mediaLinks;

        public AnimalSearchService(IRecognitionLogRepository recognitionLogs, IMediaLinkService mediaLinks)
        {
            this.recognitionLogs = recognitionLogs;
            this.mediaLinks = mediaLinks;
        }

        public async Task<IReadOnlyList<AnimalSearchResult>> SearchAsync(string? searchTerm, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new RequestRefusedException(EnterSearchTerm);
            }

            var logs = await recognitionLogs.FindByAnimalNameAsync(searchTerm.Trim(), cancellationToken);

            // Only images can be shown on the page; a video is left out.
            var matches = logs
                .Where(log => MediaFile.IsImage(log.FileName))
                .GroupBy(log => log.AnimalName)
                .Select(animal => new AnimalSearchResult
                {
                    AnimalName = animal.Key,
                    Count = animal.Count(),
                    ImagePaths = animal.Select(log => mediaLinks.CreateLink(log.UserId, log.FileName)).ToList()
                })
                .ToList();

            if (matches.Count == 0)
            {
                throw new NotFoundException(NoMatches);
            }

            var mostRecognised = matches.Max(match => match.Count);

            foreach (var match in matches)
            {
                match.Accuracy = (float)match.Count / mostRecognised;
            }

            return matches
                .Where(match => match.Accuracy >= MinAccuracy)
                .OrderByDescending(match => match.Accuracy)
                .ToList();
        }
    }
}
