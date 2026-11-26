namespace AnimalClassifier.Core.Recognitions.Achievements
{
    /// <summary>
    /// Something a user does with their recognitions, which earns them a badge
    /// once they have done it as many times as its goal. Every badge there is
    /// is listed in <see cref="Badges"/>.
    /// </summary>
    public abstract class Badge
    {
        protected Badge(string id, string name, string description, int goal)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfLessThan(goal, 1);

            Id = id;
            Name = name;
            Description = description;
            Goal = goal;
        }

        /// <summary>
        /// What the frontend knows it by, such as <c>"first-find"</c>, which
        /// stays the same whatever it is called.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Written to be shown as it stands.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// What earns it, written to be shown as it stands.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// How many times it is to be done, at least once.
        /// </summary>
        public int Goal { get; }

        /// <summary>
        /// How far the user is towards it, which is no more than the goal, and
        /// when they reached the goal; null while they have not.
        /// </summary>
        public abstract (int Progress, DateTime? DateEarned) Measure(AchievementFacts facts);
    }
}
