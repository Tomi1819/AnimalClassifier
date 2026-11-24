namespace AnimalClassifier.Core.Recognitions.Achievements
{
    using AnimalClassifier.Core.Recognitions.Achievements.Models;

    /// <summary>
    /// Works out a user's achievements from their recognitions, by the rules
    /// <see cref="Levels"/> and <see cref="Badges"/> set. It reads nothing
    /// itself, so the same facts always give the same achievements.
    /// </summary>
    public static class AchievementEvaluator
    {
        public static AchievementProgress Evaluate(AchievementFacts facts)
        {
            ArgumentNullException.ThrowIfNull(facts);

            // When each animal was first found, in the order they were: the
            // recognitions come in the order they were made, so the first of
            // each animal's is the earliest.
            var firstFinds = facts.Finds
                .DistinctBy(r => facts.KnownAnimal(r.FoundAnimal))
                .Select(r => r.DateRecognized)
                .ToList();

            var levels = Levels.For(facts.KnownCount)
                .Select((level, index) => new AchievementLevel
                {
                    Number = index + 1,
                    Name = level.Name,
                    AnimalsNeeded = level.AnimalsNeeded,
                    IsReached = firstFinds.Count >= level.AnimalsNeeded,
                    DateReached = level.AnimalsNeeded > 0 && firstFinds.Count >= level.AnimalsNeeded
                        ? firstFinds[level.AnimalsNeeded - 1]
                        : null
                })
                .ToList();

            return new AchievementProgress
            {
                FoundCount = firstFinds.Count,
                KnownCount = facts.KnownCount,
                Level = levels.Last(level => level.IsReached),
                NextLevel = levels.FirstOrDefault(level => !level.IsReached),
                Levels = levels,
                Badges = Badges.All.Select(badge => Measure(badge, facts)).ToList()
            };
        }

        private static BadgeProgress Measure(Badge badge, AchievementFacts facts)
        {
            var (progress, dateEarned) = badge.Measure(facts);

            return new BadgeProgress
            {
                Id = badge.Id,
                Name = badge.Name,
                Description = badge.Description,
                Goal = badge.Goal,
                Progress = progress,
                IsEarned = dateEarned is not null,
                DateEarned = dateEarned
            };
        }
    }
}
