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

        /// <summary>
        /// How many images are shown of each animal, the most recent ones. An
        /// animal can be recognised in any number, and each takes a link of its
        /// own to answer with.
        /// </summary>
        public const int MaxImagesPerAnimal = 12;

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

            // Only images can be shown on the page; a video is left out.
            var matches = await recognitionLogs.CountByAnimalNameAsync(searchTerm.Trim(), MediaFile.ImageExtensions, cancellationToken);

            if (matches.Count == 0)
            {
                throw new NotFoundException(NoMatches);
            }

            var mostRecognised = matches.Max(match => match.Count);

            var shown = matches
                .Select(match => new AnimalSearchResult
                {
                    AnimalName = match.AnimalName,
                    Count = match.Count,
                    Accuracy = (float)match.Count / mostRecognised
                })
                .Where(match => match.Accuracy >= MinAccuracy)
                .ToList();

            var latest = await recognitionLogs.GetLatestByAnimalsAsync(
                shown.Select(match => match.AnimalName), MediaFile.ImageExtensions, MaxImagesPerAnimal, cancellationToken);

            // Matched to the animals the way the database grouped them, whose
            // collation ignores case.
            var imagesByAnimal = latest.ToLookup(log => log.AnimalName, StringComparer.OrdinalIgnoreCase);

            foreach (var match in shown)
            {
                match.ImagePaths = imagesByAnimal[match.AnimalName]
                    .Select(log => mediaLinks.CreateLink(log.UserId, log.FileName))
                    .ToList();
            }

            return shown;
        }
    }
}
