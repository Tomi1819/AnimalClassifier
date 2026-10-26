namespace AnimalClassifier.Core.Recognitions.Collection.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// What one recognition did for its user's collection, and how far the
    /// collection is now.
    /// </summary>
    public class CollectionFind
    {
        /// <summary>
        /// Written by its name, such as <c>"FirstFind"</c>.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter<FindStatus>))]
        public FindStatus Status { get; set; }

        /// <summary>
        /// The animal it finds, or would once the user says the model was
        /// right, as the model writes it. Null when it finds none.
        /// </summary>
        public string? AnimalName { get; set; }

        /// <summary>
        /// How many animals the user has found, this one's included.
        /// </summary>
        public int FoundCount { get; set; }

        /// <summary>
        /// How many animals the model knows, which is how many there are to
        /// find.
        /// </summary>
        public int KnownCount { get; set; }
    }
}
