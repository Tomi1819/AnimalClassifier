namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;

    /// <summary>
    /// The feedback users give on their recognitions, at most one each. It is
    /// removed with its recognition, by the database, so nothing here removes
    /// a user's feedback in bulk.
    /// </summary>
    public interface IRecognitionFeedbackRepository
    {
        /// <summary>
        /// Adds a feedback, which is stored on the next save.
        /// </summary>
        void Add(RecognitionFeedback feedback);

        /// <summary>
        /// Removes a feedback, which goes on the next save.
        /// </summary>
        void Remove(RecognitionFeedback feedback);

        /// <summary>
        /// The feedback on one recognition, if there is any and the
        /// recognition belongs to the user. It is tracked, so a change made to
        /// it is stored on the next save.
        /// </summary>
        Task<RecognitionFeedback?> FindForUserAsync(string userId, int recognitionId);
    }
}
