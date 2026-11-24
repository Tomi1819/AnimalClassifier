namespace AnimalClassifier.Core.Recognitions.Achievements
{
    /// <summary>
    /// A badge earned on the day something has happened as many times as its
    /// goal, whatever happened in between.
    /// </summary>
    public sealed class CountedBadge : Badge
    {
        private readonly Func<AchievementFacts, IEnumerable<DateTime>> occasions;

        /// <param name="occasions">When each of the times it happened was, in any order.</param>
        public CountedBadge(string id, string name, string description, int goal, Func<AchievementFacts, IEnumerable<DateTime>> occasions)
            : base(id, name, description, goal)
        {
            ArgumentNullException.ThrowIfNull(occasions);

            this.occasions = occasions;
        }

        public override (int Progress, DateTime? DateEarned) Measure(AchievementFacts facts)
        {
            var first = occasions(facts).Order().Take(Goal).ToList();

            return (first.Count, first.Count == Goal ? first[^1] : null);
        }
    }
}
