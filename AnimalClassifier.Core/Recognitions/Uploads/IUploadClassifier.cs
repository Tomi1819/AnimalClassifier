namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Classification.Models;

    /// <summary>
    /// Runs the model on an upload once it has its turn; see
    /// <see cref="IClassificationLimiter"/>. Decoding takes as much as
    /// classifying, so it happens within the same turn. Waiting, and a video
    /// between its frames, stop when the caller goes away.
    /// </summary>
    public interface IUploadClassifier
    {
        /// <summary>
        /// Encodes the image afresh, as <see cref="ImageSanitizer"/> does, and
        /// classifies what it encoded.
        /// </summary>
        /// <param name="extension">The format to encode it in, such as <c>.jpg</c>.</param>
        /// <returns>The image as encoded afresh, which is what is stored, and the animal in it.</returns>
        /// <exception cref="RequestRefusedException">
        /// When the image has too many pixels, or cannot be decoded.
        /// </exception>
        /// <exception cref="ServiceBusyException">
        /// When too many uploads are waiting for a turn already.
        /// </exception>
        Task<(byte[] Image, Prediction Prediction)> ClassifyImageAsync(byte[] upload, string extension, CancellationToken cancellationToken);

        /// <summary>
        /// Classifies the frames sampled from a stored video, one at a time.
        /// </summary>
        /// <returns>The animal seen in each frame, in the order they show.</returns>
        /// <exception cref="RequestRefusedException">
        /// When the file cannot be read as a video, or its frames are too large.
        /// </exception>
        /// <exception cref="ServiceBusyException">
        /// When too many uploads are waiting for a turn already.
        /// </exception>
        Task<IReadOnlyList<Prediction>> ClassifyVideoAsync(string videoPath, CancellationToken cancellationToken);
    }
}
