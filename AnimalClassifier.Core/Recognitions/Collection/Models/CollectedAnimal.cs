namespace AnimalClassifier.Core.Recognitions.Collection.Models
{
    using AnimalClassifier.Core.Recognitions.Media;

    /// <summary>
    /// One animal the model knows, and what the user has found of it.
    /// </summary>
    public class CollectedAnimal
    {
        /// <summary>
        /// As the model writes it.
        /// </summary>
        public string AnimalName { get; set; } = string.Empty;

        /// <summary>
        /// How many of the user's recognitions found it, videos and cleared
        /// ones as well; none for an animal not found yet.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// When the first of the user's recognitions to find it was made; null
        /// for an animal not found yet.
        /// </summary>
        public DateTime? DateFirstRecognized { get; set; }

        /// <summary>
        /// A link to the image that shows it best, among those still in the
        /// user's history that found it: one the user said shows it before one
        /// only the model was sure of, and then the one the model was surest
        /// of. It runs out after an hour; see <see cref="IMediaLinkService"/>.
        /// Null when there is none, as for an animal found only in videos, or
        /// in images since cleared.
        /// </summary>
        public string? ImagePath { get; set; }
    }
}
