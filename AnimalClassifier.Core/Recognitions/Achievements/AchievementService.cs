namespace AnimalClassifier.Core.Recognitions.Achievements
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Achievements.Models;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Collection;
    using Microsoft.Extensions.Options;
    using static AnimalClassifier.Core.Recognitions.Achievements.AchievementMessages;

    // Nothing is stored: the user's recognitions are read in one query, a
    // short row each, and the rules run over them here, where they can be
    // tested without a database. What one recognition earned is what the
    // user has with it and would not have without it, so nothing needs to
    // remember what they were told before.
    public class AchievementService : IAchievementService
    {
        private readonly IRecognitionAchievementQueries recognitionAchievements;
        private readonly IImageClassifier classifier;
        private readonly CollectionSettings collectionSettings;

        public AchievementService(IRecognitionAchievementQueries recognitionAchievements,
                                  IImageClassifier classifier,
                                  IOptions<CollectionSettings> collectionSettingsOptions)
        {
            this.recognitionAchievements = recognitionAchievements;
            this.classifier = classifier;
            collectionSettings = collectionSettingsOptions.Value;
        }

        public async Task<AchievementProgress> GetAchievementsAsync(string userId, string? timeZoneId, CancellationToken cancellationToken) =>
            AchievementEvaluator.Evaluate(await ReadFactsAsync(userId, timeZoneId, cancellationToken));

        public async Task<RecognitionAchievements> GetRecognitionAchievementsAsync(string userId, int recognitionId, string? timeZoneId, CancellationToken cancellationToken)
        {
            var facts = await ReadFactsAsync(userId, timeZoneId, cancellationToken);

            if (!facts.Contains(recognitionId))
            {
                throw new NotFoundException(RecognitionNotFound);
            }

            var withIt = AchievementEvaluator.Evaluate(facts);
            var withoutIt = AchievementEvaluator.Evaluate(facts.Without(recognitionId));

            var earnedWithoutIt = withoutIt.Badges
                .Where(badge => badge.IsEarned)
                .Select(badge => badge.Id)
                .ToHashSet(StringComparer.Ordinal);

            return new RecognitionAchievements
            {
                NewLevel = withIt.Level.Number > withoutIt.Level.Number ? withIt.Level : null,
                NewBadges = withIt.Badges
                    .Where(badge => badge.IsEarned && !earnedWithoutIt.Contains(badge.Id))
                    .ToList()
            };
        }

        // The time zone is checked first, so that a request refused for it
        // reads nothing.
        private async Task<AchievementFacts> ReadFactsAsync(string userId, string? timeZoneId, CancellationToken cancellationToken)
        {
            var timeZone = UserTimeZone.Find(timeZoneId);

            var recognitions = await recognitionAchievements.GetFactsAsync(userId, collectionSettings.MinimumScore, cancellationToken);

            return new AchievementFacts(recognitions, classifier.KnownAnimals, timeZone);
        }
    }
}
