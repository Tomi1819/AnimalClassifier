namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Recognitions.Classification.Models;

    /// <summary>
    /// Tells which animal an image shows.
    /// </summary>
    public interface IImageClassifier
    {
        /// <param name="image">The encoded image, such as a JPEG file's bytes.</param>
        Prediction Classify(byte[] image);
    }
}
