namespace AnimalClassifier.Core.Recognitions.Collection.Models
{
    /// <summary>
    /// How far one user is with the collection: every animal the model knows,
    /// and which of them they have found.
    /// </summary>
    public class CollectionProgress
    {
        /// <summary>
        /// How many animals the model knows, which is how many there are to
        /// find.
        /// </summary>
        public int KnownCount { get; set; }

        /// <summary>
        /// How many of them the user has found at least once.
        /// </summary>
        public int FoundCount { get; set; }

        /// <summary>
        /// Every animal the model knows, found or not, in alphabetical order.
        /// </summary>
        public List<CollectedAnimal> Animals { get; set; } = [];
    }
}
