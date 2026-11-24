namespace AnimalClassifier.Core.Recognitions.Achievements.Models
{
    /// <summary>
    /// What one recognition earned its user: what they have now that they
    /// would not have without it.
    /// </summary>
    public class RecognitionAchievements
    {
        /// <summary>
        /// The level it took them up to; null when it took them up none.
        /// </summary>
        public AchievementLevel? NewLevel { get; set; }

        /// <summary>
        /// The badges it earned them, in the order they are shown; none when
        /// it earned none.
        /// </summary>
        public List<BadgeProgress> NewBadges { get; set; } = [];
    }
}
