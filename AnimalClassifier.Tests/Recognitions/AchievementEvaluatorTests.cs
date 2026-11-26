namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Achievements;
    using AnimalClassifier.Core.Recognitions.Achievements.Models;

    /// <summary>
    /// The rules of the levels and badges, over recognitions the test makes
    /// up, which can be dated and given feedback in ways the API tests would
    /// take many requests to.
    /// </summary>
    public class AchievementEvaluatorTests
    {
        private const string TokyoTimeZone = "Asia/Tokyo";

        private static readonly IReadOnlyList<string> KnownAnimals = new StubImageClassifier().KnownAnimals;

        private static readonly DateTime Start = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        private int nextId = 1;

        [Fact]
        public void Levels_ForAModelThatKnowsFewAnimals_LeaveOutThoseNeedingAsManyOrMore()
        {
            Assert.Equal(
                [(Levels.FirstLevelName, 0), ("Spotter", 3), (Levels.LastLevelName, 5)],
                Levels.For(5));
        }

        [Fact]
        public void Levels_ForAModelThatKnowsManyAnimals_EndWithEveryAnimalFound()
        {
            var levels = Levels.For(90);

            Assert.Equal([0, 3, 10, 20, 35, 50, 70, 90], levels.Select(level => level.AnimalsNeeded));
            Assert.Equal(Levels.LastLevelName, levels[^1].Name);
        }

        [Fact]
        public void Levels_ForAModelThatKnowsNoAnimals_AreOnlyTheFirst()
        {
            Assert.Equal([(Levels.FirstLevelName, 0)], Levels.For(0));
        }

        [Fact]
        public void Evaluate_WithoutRecognitions_IsAtTheFirstLevel_WithNoBadgeEarned()
        {
            var progress = Evaluate([]);

            Assert.Equal(0, progress.FoundCount);
            Assert.Equal(KnownAnimals.Count, progress.KnownCount);
            Assert.Equal(1, progress.Level.Number);
            Assert.True(progress.Level.IsReached);
            Assert.Null(progress.Level.DateReached);
            Assert.Equal(2, progress.NextLevel?.Number);
            Assert.Equal(Badges.All.Select(badge => badge.Id), progress.Badges.Select(badge => badge.Id));
            Assert.All(progress.Badges, badge =>
            {
                Assert.False(badge.IsEarned);
                Assert.Equal(0, badge.Progress);
                Assert.Null(badge.DateEarned);
            });
        }

        // The same animal again, in another case, is not another animal.
        [Fact]
        public void Evaluate_ReachesALevel_WhenTheLastAnimalItNeedsIsFirstFound()
        {
            var progress = Evaluate(
            [
                Recognition("Cat", day: 0),
                Recognition("cat", day: 1),
                Recognition("Dog", day: 2),
                Recognition("Fox", day: 3)
            ]);

            Assert.Equal(3, progress.FoundCount);
            Assert.Equal("Spotter", progress.Level.Name);
            Assert.Equal(Start.AddDays(3), progress.Level.DateReached);
            Assert.Equal(Levels.LastLevelName, progress.NextLevel?.Name);
        }

        [Fact]
        public void Evaluate_EveryAnimalFound_IsAtTheLastLevel_WithNoneAfterIt()
        {
            var progress = Evaluate(KnownAnimals.Select((animal, day) => Recognition(animal, day)).ToList());

            Assert.Equal(Levels.LastLevelName, progress.Level.Name);
            Assert.Null(progress.NextLevel);
            Assert.All(progress.Levels, level => Assert.True(level.IsReached));
        }

        // A video in which no animal showed clearly, and an animal an older
        // model knew.
        [Fact]
        public void Evaluate_LeavesOutAnimalsTheModelDoesNotKnow()
        {
            var progress = Evaluate([Recognition("Unknown", fileName: "clip.mp4"), Recognition("Owl")]);

            Assert.Equal(0, progress.FoundCount);
            Assert.False(Badge(progress, Badges.FirstFind).IsEarned);
            Assert.False(Badge(progress, Badges.FilmMaker).IsEarned);
        }

        [Fact]
        public void Evaluate_RecognitionThatFindsNothing_CountsForNoLevel()
        {
            var progress = Evaluate([Recognition("Cat", modelWasSure: false)]);

            Assert.Equal(0, progress.FoundCount);
            Assert.False(Badge(progress, Badges.FirstFind).IsEarned);
        }

        [Fact]
        public void FirstFind_IsEarnedByTheFirstRecognitionToFindAnAnimal()
        {
            var progress = Evaluate([Recognition("Cat", day: 0, modelWasSure: false), Recognition("Dog", day: 1)]);

            var firstFind = Badge(progress, Badges.FirstFind);
            Assert.True(firstFind.IsEarned);
            Assert.Equal(1, firstFind.Progress);
            Assert.Equal(Start.AddDays(1), firstFind.DateEarned);
        }

        [Fact]
        public void SharpEye_IsEarnedByARightAnswerTheModelWasSureEnoughOf()
        {
            var sure = Evaluate([Recognition("Cat", score: Badges.SharpEyeScore)]);
            var unsure = Evaluate([Recognition("Cat", score: MathF.BitDecrement(Badges.SharpEyeScore))]);

            Assert.True(Badge(sure, Badges.SharpEye).IsEarned);
            Assert.False(Badge(unsure, Badges.SharpEye).IsEarned);
        }

        // However sure the model was, it was wrong.
        [Fact]
        public void SharpEye_IsNotEarnedByAnAnswerTheUserCorrected()
        {
            var progress = Evaluate(
            [
                Recognition("Cat", score: 1, verdict: FeedbackVerdict.WrongAnimal, actualAnimal: "Dog"),
                Recognition("Fox", score: 1, verdict: FeedbackVerdict.UnlistedAnimal, actualAnimal: "capybara")
            ]);

            Assert.False(Badge(progress, Badges.SharpEye).IsEarned);
        }

        [Fact]
        public void FilmMaker_IsEarnedByAVideoThatFindsAnAnimal_NotByAnImage()
        {
            var video = Evaluate([Recognition("Wolf", fileName: "clip.MP4")]);
            var image = Evaluate([Recognition("Wolf")]);

            Assert.True(Badge(video, Badges.FilmMaker).IsEarned);
            Assert.False(Badge(image, Badges.FilmMaker).IsEarned);
        }

        // Given in another order than the recognitions were made, as when the
        // user goes back to an old one.
        [Fact]
        public void Teacher_IsEarnedWhenTheGoalthFeedbackWasGiven()
        {
            var goal = Badges.All.Single(badge => badge.Id == Badges.Teacher).Goal;
            var recognitions = Enumerable.Range(0, goal)
                .Select(day => Recognition("Cat", day: day, verdict: FeedbackVerdict.Correct, feedbackDay: goal - day))
                .ToList();

            var progress = Evaluate(recognitions);
            var shortOfIt = Evaluate(recognitions.Skip(1).ToList());

            Assert.Equal(Start.AddDays(goal), Badge(progress, Badges.Teacher).DateEarned);
            Assert.False(Badge(shortOfIt, Badges.Teacher).IsEarned);
            Assert.Equal(goal - 1, Badge(shortOfIt, Badges.Teacher).Progress);
        }

        [Fact]
        public void Corrector_CountsOnlyAnimalsNamedInPlaceOfTheModels()
        {
            var progress = Evaluate(
            [
                Recognition("Cat", verdict: FeedbackVerdict.WrongAnimal, actualAnimal: "Dog"),
                Recognition("Cat", verdict: FeedbackVerdict.Correct),
                Recognition("Cat", verdict: FeedbackVerdict.UnlistedAnimal, actualAnimal: "capybara")
            ]);

            Assert.Equal(1, Badge(progress, Badges.Corrector).Progress);
            Assert.Equal(3, Badge(progress, Badges.Teacher).Progress);
            Assert.True(Badge(progress, Badges.Discoverer).IsEarned);
        }

        [Fact]
        public void HelpingHand_IsEarnedWhenSharedFeedbackIsAccepted()
        {
            var accepted = Start.AddDays(4);

            var progress = Evaluate([Recognition("Cat", verdict: FeedbackVerdict.Correct, dateAccepted: accepted)]);
            var notYet = Evaluate([Recognition("Cat", verdict: FeedbackVerdict.Correct)]);

            Assert.Equal(accepted, Badge(progress, Badges.HelpingHand).DateEarned);
            Assert.False(Badge(notYet, Badges.HelpingHand).IsEarned);
        }

        // Two a day, and a day missed after the first three.
        [Fact]
        public void OnARoll_CountsTheLongestRunOfDays_UntilItReachesTheGoal()
        {
            var days = new[] { 0, 0, 1, 2, 4, 5, 6, 7, 8, 9 };

            var progress = Evaluate(days.Select(day => Recognition("Cat", day: day)).ToList());

            var onARoll = Badge(progress, Badges.OnARoll);
            Assert.False(onARoll.IsEarned);
            Assert.Equal(6, onARoll.Progress);

            var earned = Badge(Evaluate([.. days.Select(day => Recognition("Cat", day: day)), Recognition("Cat", day: 10)]), Badges.OnARoll);
            Assert.Equal(Start.AddDays(10), earned.DateEarned);
        }

        // 14:00 and 16:00 in UTC are on one day there, and either side of
        // midnight in Tokyo, which makes two days in a row of one.
        [Fact]
        public void OnARoll_CountsTheDaysInTheUsersTimeZone()
        {
            var recognitions = new[]
            {
                Recognition("Cat", at: Start.Date.AddHours(14)),
                Recognition("Cat", at: Start.Date.AddHours(16))
            };

            var utc = Badge(Evaluate(recognitions), Badges.OnARoll);
            var tokyo = Badge(Evaluate(recognitions, TimeZoneInfo.FindSystemTimeZoneById(TokyoTimeZone)), Badges.OnARoll);

            Assert.Equal(1, utc.Progress);
            Assert.Equal(2, tokyo.Progress);
        }

        [Fact]
        public void Without_LeavesOutTheRecognition_AndWhatOnlyItEarned()
        {
            var cat = Recognition("Cat", day: 0);
            var facts = Facts([cat, Recognition("Dog", day: 1), Recognition("Fox", day: 2)]);

            var without = AchievementEvaluator.Evaluate(facts.Without(cat.Id));

            Assert.False(facts.Without(cat.Id).Contains(cat.Id));
            Assert.Equal(2, without.FoundCount);
            Assert.Equal(1, without.Level.Number);
            Assert.Equal(Start.AddDays(1), Badge(without, Badges.FirstFind).DateEarned);
        }

        private static AchievementProgress Evaluate(IReadOnlyList<RecognitionFacts> recognitions, TimeZoneInfo? timeZone = null) =>
            AchievementEvaluator.Evaluate(Facts(recognitions, timeZone));

        // In the order they were made, as the query reads them.
        private static AchievementFacts Facts(IReadOnlyList<RecognitionFacts> recognitions, TimeZoneInfo? timeZone = null) =>
            new(recognitions.OrderBy(r => r.DateRecognized).ThenBy(r => r.Id).ToList(), KnownAnimals, timeZone ?? TimeZoneInfo.Utc);

        private static BadgeProgress Badge(AchievementProgress progress, string id) =>
            progress.Badges.Single(badge => badge.Id == id);

        /// <param name="day">How many days after the start it was made.</param>
        /// <param name="modelWasSure">Whether the model was sure enough for the collection to take its word, without feedback.</param>
        /// <param name="feedbackDay">How many days after the start the feedback was given; the day it was made unless given.</param>
        private RecognitionFacts Recognition(
            string animal,
            int day = 0,
            DateTime? at = null,
            string? fileName = null,
            float score = 0.9f,
            bool modelWasSure = true,
            FeedbackVerdict? verdict = null,
            string? actualAnimal = null,
            int? feedbackDay = null,
            DateTime? dateAccepted = null) =>
            new()
            {
                Id = nextId++,
                FileName = fileName ?? "image.jpg",
                DateRecognized = at ?? Start.AddDays(day),
                RecognizedAnimal = animal,
                PredictionScore = score,
                FoundAnimal = verdict switch
                {
                    null => modelWasSure ? animal : null,
                    FeedbackVerdict.Correct => animal,
                    FeedbackVerdict.WrongAnimal => actualAnimal,
                    _ => null
                },
                Verdict = verdict,
                DateFeedbackSubmitted = verdict is null ? null : Start.AddDays(feedbackDay ?? day),
                DateAcceptedForTraining = dateAccepted
            };
    }
}
