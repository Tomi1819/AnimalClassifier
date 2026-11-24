namespace AnimalClassifier.Core.Recognitions.Achievements
{
    /// <summary>
    /// A badge earned by making a recognition on as many days in a row as its
    /// goal, the days being the user's own, in their time zone. The progress
    /// is the longest run so far rather than the current one, so that it does
    /// not depend on what day it is when it is asked for.
    /// </summary>
    public sealed class StreakBadge : Badge
    {
        public StreakBadge(string id, string name, string description, int goal)
            : base(id, name, description, goal)
        {
        }

        // The recognitions come in the order they were made, so their days do
        // too, and a run is broken by the first day missed.
        public override (int Progress, DateTime? DateEarned) Measure(AchievementFacts facts)
        {
            var longest = 0;
            var run = 0;
            DateOnly? previousDay = null;

            foreach (var recognition in facts.Recognitions)
            {
                var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(recognition.DateRecognized, facts.TimeZone));

                if (day == previousDay)
                {
                    continue;
                }

                run = day == previousDay?.AddDays(1) ? run + 1 : 1;
                previousDay = day;
                longest = Math.Max(longest, run);

                if (run == Goal)
                {
                    return (Goal, recognition.DateRecognized);
                }
            }

            return (longest, null);
        }
    }
}
