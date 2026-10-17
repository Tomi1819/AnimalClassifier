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
        /// How many times the user recognised it, in videos and cleared
        /// recognitions as well; none for an animal not found yet.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// When the user first recognised it; null for an animal not found
        /// yet.
        /// </summary>
        public DateTime? DateFirstRecognized { get; set; }

        /// <summary>
        /// A link to the image of it the model was surest of, among those
        /// still in the user's history, which runs out after an hour; see
        /// <see cref="IMediaLinkService"/>. Null when there is none, as for an
        /// animal found only in videos, or in images since cleared.
        /// </summary>
        public string? ImagePath { get; set; }
    }
}
