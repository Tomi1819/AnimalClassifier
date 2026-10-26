namespace AnimalClassifier.Core.Recognitions.Collection
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Collection.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using Microsoft.Extensions.Options;
    using static AnimalClassifier.Core.Recognitions.Collection.CollectionMessages;

    // The database counts and picks the images, so what is read grows with the
    // animals the model knows rather than with every recognition the user made.
    public class AnimalCollectionService : IAnimalCollectionService
    {
        private readonly IRecognitionCollectionQueries recognitionCollection;
        private readonly IImageClassifier classifier;
        private readonly IMediaLinkService mediaLinks;
        private readonly CollectionSettings settings;

        public AnimalCollectionService(IRecognitionCollectionQueries recognitionCollection,
                                       IImageClassifier classifier,
                                       IMediaLinkService mediaLinks,
                                       IOptions<CollectionSettings> settingsOptions)
        {
            this.recognitionCollection = recognitionCollection;
            this.classifier = classifier;
            this.mediaLinks = mediaLinks;
            settings = settingsOptions.Value;
        }

        public async Task<CollectionProgress> GetCollectionAsync(string userId, CancellationToken cancellationToken)
        {
            var finds = await CountFindsAsync(userId, cancellationToken);
            var found = classifier.KnownAnimals.Where(finds.Contains).ToList();

            // Only an image can be shown on a card; a video is left out. The
            // database picks one for each animal at most.
            var bestImages = (await recognitionCollection.GetBestFindsAsync(userId, found, MediaFile.ImageExtensions, settings.MinimumScore, cancellationToken))
                .ToLookup(image => image.AnimalName, image => image.FileName, StringComparer.OrdinalIgnoreCase);

            return new CollectionProgress
            {
                KnownCount = classifier.KnownAnimals.Count,
                FoundCount = found.Count,
                Animals = classifier.KnownAnimals
                    .Select(animalName =>
                    {
                        var counts = finds[animalName];
                        var bestImage = bestImages[animalName].FirstOrDefault();

                        return new CollectedAnimal
                        {
                            AnimalName = animalName,
                            Count = counts.Sum(animal => animal.Count),
                            DateFirstRecognized = counts.Min(animal => (DateTime?)animal.FirstFound),
                            ImagePath = bestImage is null ? null : mediaLinks.CreateLink(userId, bestImage)
                        };
                    })
                    .ToList()
            };
        }

        public async Task<CollectionFind> GetFindAsync(string userId, int recognitionId, CancellationToken cancellationToken)
        {
            var find = await recognitionCollection.GetFindAsync(userId, recognitionId, settings.MinimumScore, cancellationToken)
                ?? throw new NotFoundException(RecognitionNotFound);

            var finds = await CountFindsAsync(userId, cancellationToken);

            // Feedback that finds nothing says the image shows an animal the
            // model does not know, so only a recognition without any waits to
            // be confirmed.
            var animalName = KnownAnimal(find.FoundAnimal ?? (find.HasFeedback ? null : find.RecognizedAnimal));

            var status = animalName switch
            {
                null => FindStatus.None,
                _ when find.FoundAnimal is null => finds.Contains(animalName) ? FindStatus.AlreadyFound : FindStatus.Unconfirmed,
                _ => find.IsFirstFind ? FindStatus.FirstFind : FindStatus.AlreadyFound
            };

            return new CollectionFind
            {
                Status = status,
                AnimalName = animalName,
                FoundCount = classifier.KnownAnimals.Count(finds.Contains),
                KnownCount = classifier.KnownAnimals.Count
            };
        }

        // Matched to the animals the model knows, as it writes them, the way
        // the database grouped them, whose collation ignores case. A name it
        // does not know, such as a video's Unknown, matches none of them.
        private async Task<ILookup<string, (string AnimalName, int Count, DateTime FirstFound)>> CountFindsAsync(string userId, CancellationToken cancellationToken) =>
            (await recognitionCollection.CountFindsByAnimalAsync(userId, settings.MinimumScore, cancellationToken))
                .ToLookup(animal => animal.AnimalName, StringComparer.OrdinalIgnoreCase);

        private string? KnownAnimal(string? animalName) =>
            classifier.KnownAnimals.FirstOrDefault(known => string.Equals(known, animalName, StringComparison.OrdinalIgnoreCase));
    }
}
