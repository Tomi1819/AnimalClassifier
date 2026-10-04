namespace AnimalClassifier.Core.Recognitions.Training.Models
{
    /// <summary>
    /// What the feedback says of the model as a whole: how often users agree
    /// with it, the animals it takes for others, and those it does not know
    /// that users looked for. It counts all of it, whether or not it allows
    /// training, as none of it names whose it is.
    /// </summary>
    public class FeedbackSummary
    {
        public int TotalCount { get; set; }

        public int CorrectCount { get; set; }

        public int WrongAnimalCount { get; set; }

        public int UnlistedAnimalCount { get; set; }

        /// <summary>
        /// How much of the feedback that allows training waits for review.
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// How much of the feedback that allows training was accepted, which
        /// is what an export holds.
        /// </summary>
        public int AcceptedCount { get; set; }

        public int RejectedCount { get; set; }

        /// <summary>
        /// The animals the model took for others most often, most first.
        /// </summary>
        public List<CommonMistake> CommonMistakes { get; set; } = [];

        /// <summary>
        /// The animals the model does not know that users named most, most
        /// first, which are the ones to add to it.
        /// </summary>
        public List<RequestedAnimal> RequestedAnimals { get; set; } = [];
    }
}
