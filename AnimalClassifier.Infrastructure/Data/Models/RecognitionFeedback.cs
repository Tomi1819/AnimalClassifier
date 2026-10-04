namespace AnimalClassifier.Infrastructure.Data.Models
{
    /// <summary>
    /// What a user said of one of their recognitions: whether the model named
    /// the right animal, and which it was when it did not. A recognition has
    /// at most one, which its owner can change or withdraw, and which goes
    /// when the recognition does.
    /// </summary>
    public class RecognitionFeedback
    {
        /// <summary>
        /// The longest <see cref="ActualAnimal"/> kept.
        /// </summary>
        public const int MaxActualAnimalLength = 50;

        /// <summary>
        /// The longest <see cref="Comment"/> kept.
        /// </summary>
        public const int MaxCommentLength = 500;

        public int Id { get; set; }

        public int RecognitionId { get; set; }
        public AnimalRecognitionLog Recognition { get; set; } = null!;

        public FeedbackVerdict Verdict { get; set; }

        /// <summary>
        /// The animal the image shows, when the model named another: one the
        /// model knows for <see cref="FeedbackVerdict.WrongAnimal"/>, and the
        /// user's own word for <see cref="FeedbackVerdict.UnlistedAnimal"/>.
        /// Null when the model was right.
        /// </summary>
        public string? ActualAnimal { get; set; }

        public string? Comment { get; set; }

        /// <summary>
        /// Whether the user agreed to the image being used to train the model.
        /// Without it, the feedback only counts towards what administrators
        /// are told of the model's mistakes, and nobody reviews it.
        /// </summary>
        public bool AllowsTraining { get; set; }

        public FeedbackReviewStatus ReviewStatus { get; set; }

        /// <summary>
        /// When it was given, or last changed.
        /// </summary>
        public DateTime DateSubmitted { get; set; }

        /// <summary>
        /// When it was last accepted or rejected; null while it waits.
        /// </summary>
        public DateTime? DateReviewed { get; set; }
    }
}
