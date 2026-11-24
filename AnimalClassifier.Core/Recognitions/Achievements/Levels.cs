namespace AnimalClassifier.Core.Recognitions.Achievements
{
    /// <summary>
    /// The levels a user goes up through by finding animals they had not found
    /// before, as the collection counts them. The last is reached by finding
    /// every animal the model knows, however many that is; a level of the
    /// others that would need as many, or more, is left out, so that a model
    /// that knows fewer animals has fewer levels.
    /// </summary>
    public static class Levels
    {
        public const string FirstLevelName = "Newcomer";
        public const string LastLevelName = "Master of the Wild";

        // Between the first and the last, each with how many animals it needs.
        private static readonly (string Name, int AnimalsNeeded)[] Steps =
        [
            ("Spotter", 3),
            ("Explorer", 10),
            ("Tracker", 20),
            ("Ranger", 35),
            ("Naturalist", 50),
            ("Zoologist", 70)
        ];

        /// <summary>
        /// The levels there are when the model knows as many animals, from
        /// the first, which needs none.
        /// </summary>
        public static IReadOnlyList<(string Name, int AnimalsNeeded)> For(int knownCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(knownCount);

            List<(string Name, int AnimalsNeeded)> levels = [(FirstLevelName, 0)];
            levels.AddRange(Steps.Where(step => step.AnimalsNeeded < knownCount));

            if (knownCount > 0)
            {
                levels.Add((LastLevelName, knownCount));
            }

            return levels;
        }
    }
}
