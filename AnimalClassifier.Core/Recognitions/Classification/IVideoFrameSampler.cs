namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Common.Exceptions;

    /// <summary>
    /// Picks the frames of a video that are classified, so that a video can be
    /// recognised one image at a time.
    /// </summary>
    public interface IVideoFrameSampler
    {
        /// <summary>
        /// One frame for every second of the video, or fewer for a long one,
        /// each encoded as an image. They are read as they are enumerated.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the file cannot be read as a video, which is reported once
        /// enumerating starts.
        /// </exception>
        IEnumerable<byte[]> SampleFrames(string videoPath);
    }
}
