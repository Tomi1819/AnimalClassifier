namespace AnimalClassifier.Core.Recognitions.Achievements.Models
{
    /// <summary>
    /// Everything one user has achieved: the level they are at, and the badges
    /// they have earned and are working towards.
    /// </summary>
    public class AchievementProgress
    {
        /// <summary>
        /// How many of the animals the model knows the user has found, which
        /// is what the levels count.
        /// </summary>
        public int FoundCount { get; set; }

        /// <summary>
        /// How many animals the model knows, which is what the last level
        /// needs.
        /// </summary>
        public int KnownCount { get; set; }

        /// <summary>
        /// The highest level the user has reached.
        /// </summary>
        public AchievementLevel Level { get; set; } = new();

        /// <summary>
        /// The one after it; null at the last.
        /// </summary>
        public AchievementLevel? NextLevel { get; set; }

        /// <summary>
        /// Every level there is, from the first.
        /// </summary>
        public List<AchievementLevel> Levels { get; set; } = [];

        /// <summary>
        /// Every badge there is, earned or not, in the order they are shown.
        /// </summary>
        public List<BadgeProgress> Badges { get; set; } = [];
    }
}
