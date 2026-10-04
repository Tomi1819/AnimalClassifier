namespace AnimalClassifier.Infrastructure.Data.Models
{
    /// <summary>
    /// What a user said of the animal the model named in their image.
    /// </summary>
    public enum FeedbackVerdict
    {
        /// <summary>
        /// The model named the right animal.
        /// </summary>
        Correct,

        /// <summary>
        /// The image shows another animal the model knows.
        /// </summary>
        WrongAnimal,

        /// <summary>
        /// The image shows an animal the model does not know, which it could
        /// not have named.
        /// </summary>
        UnlistedAnimal
    }
}
