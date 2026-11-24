namespace AnimalClassifier.Core.Recognitions.Achievements
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// Every badge there is to earn, in the order they are shown. A badge is
    /// added by adding it here; the frontend shows any it has no picture for
    /// with a plain one.
    /// </summary>
    public static class Badges
    {
        public const string FirstFind = "first-find";
        public const string SharpEye = "sharp-eye";
        public const string FilmMaker = "film-maker";
        public const string Teacher = "teacher";
        public const string Corrector = "corrector";
        public const string Discoverer = "discoverer";
        public const string HelpingHand = "helping-hand";
        public const string OnARoll = "on-a-roll";

        /// <summary>
        /// The least score at which the model's right answer earns
        /// <see cref="SharpEye"/>.
        /// </summary>
        public const float SharpEyeScore = 0.95f;

        public static IReadOnlyList<Badge> All { get; } =
        [
            new CountedBadge(FirstFind, "First find", "Find your first animal.", 1,
                facts => facts.Finds.Select(r => r.DateRecognized)),

            // Right in what it named, as the collection takes it: the user
            // said so, or did not say otherwise.
            new CountedBadge(SharpEye, "Sharp eye", "Get a right answer the model was at least 95% sure of.", 1,
                facts => facts.Finds
                    .Where(r => r.PredictionScore >= SharpEyeScore
                        && facts.KnownAnimal(r.FoundAnimal) == facts.KnownAnimal(r.RecognizedAnimal))
                    .Select(r => r.DateRecognized)),

            new CountedBadge(FilmMaker, "Film-maker", "Find an animal in a video.", 1,
                facts => facts.Finds.Where(r => MediaFile.IsVideo(r.FileName)).Select(r => r.DateRecognized)),

            new CountedBadge(Teacher, "Teacher", "Tell us whether the model was right 10 times.", 10,
                facts => DatesOfFeedback(facts, verdict: null)),

            new CountedBadge(Corrector, "Corrector", "Name the right animal 5 times when the model was wrong.", 5,
                facts => DatesOfFeedback(facts, FeedbackVerdict.WrongAnimal)),

            new CountedBadge(Discoverer, "Discoverer", "Show the model an animal it does not know yet.", 1,
                facts => DatesOfFeedback(facts, FeedbackVerdict.UnlistedAnimal)),

            new CountedBadge(HelpingHand, "Helping hand", "Share an image to train the model on, and have it accepted.", 1,
                facts => facts.Recognitions
                    .Where(r => r.DateAcceptedForTraining is not null)
                    .Select(r => r.DateAcceptedForTraining!.Value)),

            new StreakBadge(OnARoll, "On a roll", "Recognise animals 7 days in a row.", 7)
        ];

        // Feedback with the verdict, or with any when it is null, dated when it
        // was last given, since changing it is giving it again.
        private static IEnumerable<DateTime> DatesOfFeedback(AchievementFacts facts, FeedbackVerdict? verdict) =>
            facts.Recognitions
                .Where(r => r.Verdict is not null && (verdict is null || r.Verdict == verdict))
                .Select(r => r.DateFeedbackSubmitted!.Value);
    }
}
