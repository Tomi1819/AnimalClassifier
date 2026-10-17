namespace AnimalClassifier.Core.Recognitions.Collection
{
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Collection.Models;
    using AnimalClassifier.Core.Recognitions.Media;

    // The database counts and picks the images, so what is read grows with the
    // animals the model knows rather than with every recognition the user made.
    public class AnimalCollectionService : IAnimalCollectionService
    {
        private readonly IRecognitionCollectionQueries recognitionCollection;
        private readonly IImageClassifier classifier;
        private readonly IMediaLinkService mediaLinks;

        public AnimalCollectionService(IRecognitionCollectionQueries recognitionCollection,
                                       IImageClassifier classifier,
                                       IMediaLinkService mediaLinks)
        {
            this.recognitionCollection = recognitionCollection;
            this.classifier = classifier;
            this.mediaLinks = mediaLinks;
        }

        public async Task<CollectionProgress> GetCollectionAsync(string userId, CancellationToken cancellationToken)
        {
            // Matched to the animals the model knows, as it writes them, the
            // way the database grouped them, whose collation ignores case. A
            // name it does not know, such as a video's Unknown, is left out.
            var recognised = (await recognitionCollection.CountByAnimalAsync(userId, cancellationToken))
                .ToLookup(animal => animal.AnimalName, StringComparer.OrdinalIgnoreCase);

            var found = classifier.KnownAnimals.Where(recognised.Contains).ToList();

            // Only an image can be shown on a card; a video is left out.
            var bestImages = (await recognitionCollection.GetBestByAnimalsAsync(userId, found, MediaFile.ImageExtensions, cancellationToken))
                .ToLookup(log => log.AnimalName, StringComparer.OrdinalIgnoreCase);

            return new CollectionProgress
            {
                KnownCount = classifier.KnownAnimals.Count,
                FoundCount = found.Count,
                Animals = classifier.KnownAnimals
                    .Select(animalName =>
                    {
                        var counts = recognised[animalName];
                        var bestImage = bestImages[animalName].MaxBy(log => log.PredictionScore);

                        return new CollectedAnimal
                        {
                            AnimalName = animalName,
                            Count = counts.Sum(animal => animal.Count),
                            DateFirstRecognized = counts.Min(animal => (DateTime?)animal.FirstRecognized),
                            ImagePath = bestImage is null ? null : mediaLinks.CreateLink(bestImage.UserId, bestImage.FileName)
                        };
                    })
                    .ToList()
            };
        }
    }
}
