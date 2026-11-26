namespace AnimalClassifier.Core.Recognitions.Achievements.Models
{
    /// <summary>
    /// One of the levels there are, and whether the user has reached it; see
    /// <see cref="Levels"/>.
    /// </summary>
    public class AchievementLevel
    {
        /// <summary>
        /// Its place among the levels, counted from 1.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Written to be shown as it stands.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// How many animals are to be found to reach it.
        /// </summary>
        public int AnimalsNeeded { get; set; }

        public bool IsReached { get; set; }

        /// <summary>
        /// When the recognition was made that found the last of the animals it
        /// needs. Null for one not reached yet, and for the first, which needs
        /// none.
        /// </summary>
        public DateTime? DateReached { get; set; }
    }
}
