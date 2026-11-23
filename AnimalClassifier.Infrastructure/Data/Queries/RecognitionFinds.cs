namespace AnimalClassifier.Infrastructure.Data.Queries
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Queries;

    /// <summary>
    /// Which animal each recognition finds, by the rule
    /// <see cref="IRecognitionCollectionQueries"/> sets out. Every query that
    /// counts what a user has found reads through this, so that no two of them
    /// can count by different rules.
    /// </summary>
    internal static class RecognitionFinds
    {
        /// <summary>
        /// Each of the recognitions with the animal it finds. Among those that
        /// find an animal, one with feedback is one whose user said it shows
        /// that animal.
        /// </summary>
        public static IQueryable<RecognitionFind> SelectFinds(this IQueryable<AnimalRecognitionLog> logs, float minimumScore) =>
            logs.Select(l => new RecognitionFind
            {
                Id = l.Id,
                FileName = l.FileName,
                DateRecognized = l.DateRecognized,
                PredictionScore = l.PredictionScore,
                RecognizedAnimal = l.AnimalName,
                HasFeedback = l.Feedback != null,
                AnimalName = l.Feedback == null
                    ? (l.PredictionScore >= minimumScore ? l.AnimalName : null)
                    : l.Feedback.Verdict == FeedbackVerdict.Correct ? l.AnimalName
                    : l.Feedback.Verdict == FeedbackVerdict.WrongAnimal ? l.Feedback.ActualAnimal
                    : null
            });
    }
}
