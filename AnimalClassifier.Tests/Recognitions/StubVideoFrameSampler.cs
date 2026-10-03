namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Classification;
    using static AnimalClassifier.Core.Recognitions.Classification.ClassificationMessages;

    /// <summary>
    /// Stands in for reading a video, so that an uploaded one need not be a
    /// real video. It gives as many frames as a test has set, which the
    /// classifier then sees whatever it was told to in.
    /// </summary>
    public class StubVideoFrameSampler : IVideoFrameSampler
    {
        public int Frames { get; set; } = 5;

        /// <summary>
        /// Set to refuse the video, as a file that is not one is refused.
        /// </summary>
        public bool Unreadable { get; set; }

        public IEnumerable<byte[]> SampleFrames(string videoPath)
        {
            if (Unreadable)
            {
                throw new RequestRefusedException(UnreadableVideo);
            }

            return Enumerable.Range(0, Frames).Select(_ => new byte[] { 0xFF, 0xD8, 0xFF });
        }
    }
}
