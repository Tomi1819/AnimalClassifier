namespace AnimalClassifier.Core.Recognitions.Training
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Recognitions.Training.Models;

    public class FeedbackSummaryService : IFeedbackSummaryService
    {
        /// <summary>
        /// How many of the most common mistakes, and of the animals users
        /// named most, are listed.
        /// </summary>
        public const int ListedCount = 10;

        private readonly IFeedbackSummaryQueries feedbackSummary;

        public FeedbackSummaryService(IFeedbackSummaryQueries feedbackSummary)
        {
            this.feedbackSummary = feedbackSummary;
        }

        public async Task<FeedbackSummary> GetSummaryAsync(CancellationToken cancellationToken)
        {
            var verdicts = await feedbackSummary.CountByVerdictAsync(cancellationToken);
            var reviews = await feedbackSummary.CountForReviewByStatusAsync(cancellationToken);
            var mistakes = await feedbackSummary.GetMostCommonMistakesAsync(ListedCount, cancellationToken);
            var requested = await feedbackSummary.GetMostNamedUnlistedAnimalsAsync(ListedCount, cancellationToken);

            return new FeedbackSummary
            {
                TotalCount = verdicts.Values.Sum(),
                CorrectCount = verdicts.GetValueOrDefault(FeedbackVerdict.Correct),
                WrongAnimalCount = verdicts.GetValueOrDefault(FeedbackVerdict.WrongAnimal),
                UnlistedAnimalCount = verdicts.GetValueOrDefault(FeedbackVerdict.UnlistedAnimal),
                PendingCount = reviews.GetValueOrDefault(FeedbackReviewStatus.Pending),
                AcceptedCount = reviews.GetValueOrDefault(FeedbackReviewStatus.Accepted),
                RejectedCount = reviews.GetValueOrDefault(FeedbackReviewStatus.Rejected),
                CommonMistakes = mistakes
                    .Select(mistake => new CommonMistake
                    {
                        RecognizedAnimal = mistake.RecognizedAnimal,
                        ActualAnimal = mistake.ActualAnimal,
                        Count = mistake.Count
                    })
                    .ToList(),
                RequestedAnimals = requested
                    .Select(animal => new RequestedAnimal { Animal = animal.Animal, Count = animal.Count })
                    .ToList()
            };
        }
    }
}
