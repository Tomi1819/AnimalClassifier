namespace AnimalClassifier.Core.Recognitions.Collection
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// How sure the model has to be of an animal for the collection to take
    /// its word for it; see <see cref="IAnimalCollectionService"/>.
    /// </summary>
    public class CollectionSettings : ISettings
    {
        public static string SectionName => "Collection";

        /// <summary>
        /// The least score at which a recognition the user said nothing of
        /// finds its animal. Below it, the animal is found once the user says
        /// the model was right. Half by default, which is where the dashboard
        /// starts calling the model not confident.
        /// </summary>
        [Range(0.0, 1.0)]
        public float MinimumScore { get; set; } = 0.5f;
    }
}
