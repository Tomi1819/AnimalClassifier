namespace AnimalClassifier.Core.Recognitions.Achievements
{
    using AnimalClassifier.Core.Data.Queries;

    /// <summary>
    /// One user's recognitions, as the rules of their achievements read them:
    /// with the animals the model knows, and the time zone their days start
    /// and end in.
    /// </summary>
    public sealed class AchievementFacts
    {
        // Each animal the model knows by its name in any case, to the name as
        // the model writes it, matched the way the database's collation does.
        private readonly Dictionary<string, string> knownAnimals;

        /// <param name="recognitions">In the order they were made, as <see cref="IRecognitionAchievementQueries"/> reads them.</param>
        public AchievementFacts(IReadOnlyList<RecognitionFacts> recognitions, IEnumerable<string> knownAnimals, TimeZoneInfo timeZone)
            : this(recognitions, knownAnimals.DistinctBy(animal => animal, StringComparer.OrdinalIgnoreCase)
                                             .ToDictionary(animal => animal, StringComparer.OrdinalIgnoreCase), timeZone)
        {
        }

        private AchievementFacts(IReadOnlyList<RecognitionFacts> recognitions, Dictionary<string, string> knownAnimals, TimeZoneInfo timeZone)
        {
            Recognitions = recognitions;
            this.knownAnimals = knownAnimals;
            TimeZone = timeZone;
        }

        /// <summary>
        /// Every one of the user's recognitions, cleared ones and videos
        /// included, in the order they were made.
        /// </summary>
        public IReadOnlyList<RecognitionFacts> Recognitions { get; }

        public TimeZoneInfo TimeZone { get; }

        /// <summary>
        /// How many animals the model knows, which is how many there are to
        /// find.
        /// </summary>
        public int KnownCount => knownAnimals.Count;

        /// <summary>
        /// The recognitions that find an animal the model knows. A video in
        /// which no animal showed clearly finds none, nor does one of an
        /// animal an older model knew and this one does not.
        /// </summary>
        public IEnumerable<RecognitionFacts> Finds => Recognitions.Where(r => KnownAnimal(r.FoundAnimal) is not null);

        /// <summary>
        /// The animal as the model writes it, whatever its case; null when the
        /// model does not know it.
        /// </summary>
        public string? KnownAnimal(string? animalName) =>
            animalName is not null && knownAnimals.TryGetValue(animalName, out var known) ? known : null;

        public bool Contains(int recognitionId) => Recognitions.Any(r => r.Id == recognitionId);

        /// <summary>
        /// The same facts as if the recognition had never been made, which
        /// tells what it earned.
        /// </summary>
        public AchievementFacts Without(int recognitionId) =>
            new(Recognitions.Where(r => r.Id != recognitionId).ToList(), knownAnimals, TimeZone);
    }
}
