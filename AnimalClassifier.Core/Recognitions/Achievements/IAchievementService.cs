namespace AnimalClassifier.Core.Recognitions.Achievements
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Achievements.Models;

    /// <summary>
    /// The levels and badges one user earns with their recognitions. They are
    /// worked out afresh from the recognitions each time, with the animals
    /// found as the collection counts them, so they change as the collection
    /// does: saying the model was right, or naming another animal, can earn
    /// one, and deleting a recognition can take back what it earned. No other
    /// user's recognitions change them.
    /// </summary>
    public interface IAchievementService
    {
        /// <param name="timeZoneId">
        /// An IANA name, such as <c>Europe/Sofia</c>, for the days counted in
        /// a row; null for UTC.
        /// </param>
        /// <exception cref="RequestRefusedException">When no time zone has the name.</exception>
        Task<AchievementProgress> GetAchievementsAsync(string userId, string? timeZoneId, CancellationToken cancellationToken);

        /// <summary>
        /// What one of the user's recognitions earned them, as it stands now,
        /// which the dashboard tells them once it is made, and again once
        /// they say whether the model was right.
        /// </summary>
        /// <param name="timeZoneId">As for <see cref="GetAchievementsAsync"/>.</param>
        /// <exception cref="RequestRefusedException">When no time zone has the name.</exception>
        /// <exception cref="NotFoundException">
        /// When it does not exist, or belongs to another user. The two are not
        /// told apart, so that ids cannot be tested for by asking.
        /// </exception>
        Task<RecognitionAchievements> GetRecognitionAchievementsAsync(string userId, int recognitionId, string? timeZoneId, CancellationToken cancellationToken);
    }
}
