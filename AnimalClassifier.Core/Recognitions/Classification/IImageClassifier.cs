namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Recognitions.Classification.Models;

    /// <summary>
    /// Tells which animal an image shows.
    /// </summary>
    public interface IImageClassifier
    {
        /// <summary>
        /// Every animal it can name, in alphabetical order. These are the
        /// animals it was trained on, so a model trained on more knows more.
        /// </summary>
        IReadOnlyList<string> KnownAnimals { get; }

        /// <param name="image">The encoded image, such as a JPEG file's bytes.</param>
        Prediction Classify(byte[] image);
    }
}
