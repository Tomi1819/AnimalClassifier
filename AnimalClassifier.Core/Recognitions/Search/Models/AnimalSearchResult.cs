namespace AnimalClassifier.Core.Recognitions.Search.Models
{
    using AnimalClassifier.Core.Recognitions.Media;

    /// <summary>
    /// One animal that matched a search, and the images it was recognised in.
    /// </summary>
    public class AnimalSearchResult
    {
        public string AnimalName { get; set; } = string.Empty;

        /// <summary>
        /// How many images it was recognised in, by every user.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// How often it was recognised next to the match recognised most: 1
        /// for that one, and less for the rest.
        /// </summary>
        public float Accuracy { get; set; }

        /// <summary>
        /// A link to each image, which runs out after an hour; see
        /// <see cref="IMediaLinkService"/>.
        /// </summary>
        public List<string> ImagePaths { get; set; } = [];
    }
}
