namespace AnimalClassifier.Core.Recognitions.Achievements.Models
{
    /// <summary>
    /// One of the badges there are, and how far the user is towards it; see
    /// <see cref="Badges"/>.
    /// </summary>
    public class BadgeProgress
    {
        /// <summary>
        /// What the frontend knows it by, such as <c>"first-find"</c>.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Written to be shown as it stands.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// What earns it, written to be shown as it stands.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// How many times it is to be done.
        /// </summary>
        public int Goal { get; set; }

        /// <summary>
        /// How many times it has been done, up to the goal.
        /// </summary>
        public int Progress { get; set; }

        public bool IsEarned { get; set; }

        /// <summary>
        /// When the goal was reached; null while it has not been.
        /// </summary>
        public DateTime? DateEarned { get; set; }
    }
}
