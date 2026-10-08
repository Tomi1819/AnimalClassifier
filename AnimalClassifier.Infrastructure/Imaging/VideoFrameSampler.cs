namespace AnimalClassifier.Infrastructure.Imaging
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Classification;
    using OpenCvSharp;
    using static AnimalClassifier.Core.Recognitions.Classification.ClassificationMessages;

    /// <summary>
    /// Reads the frames with OpenCV.
    /// </summary>
    public class VideoFrameSampler : IVideoFrameSampler
    {
        /// <summary>
        /// The most frames read from one video, two minutes' worth at one a
        /// second. A longer video is sampled further apart instead, so that
        /// how long it takes to recognise has a bound however long it is.
        /// </summary>
        public const int MaxFrames = 120;

        /// <summary>
        /// Taken for a video that does not say how many frames it shows a
        /// second, which is the rate most video is recorded at.
        /// </summary>
        public const int AssumedFramesPerSecond = 30;

        /// <summary>
        /// The most pixels a frame can have, which is 4K's. Each frame read is
        /// decoded whole, and a small file can claim frames of any size.
        /// </summary>
        public const long MaxFramePixels = 4096 * 2160;

        // The form the frames are handed on in, which is the form of the
        // images the model was trained with.
        private const string FrameFormat = ".jpg";

        public IEnumerable<byte[]> SampleFrames(string videoPath)
        {
            using var capture = new VideoCapture(videoPath);

            if (!capture.IsOpened())
            {
                throw new RequestRefusedException(UnreadableVideo);
            }

            if ((long)capture.FrameWidth * capture.FrameHeight > MaxFramePixels)
            {
                throw new RequestRefusedException(VideoTooLarge);
            }

            var step = FramesBetweenSamples(capture.Fps, capture.FrameCount);
            var sampledAny = false;

            for (var position = 0; position < capture.FrameCount; position += step)
            {
                capture.PosFrames = position;

                using var frame = new Mat();

                // A frame that cannot be decoded is passed over rather than
                // failing the whole video.
                if (capture.Read(frame) && !frame.Empty())
                {
                    sampledAny = true;
                    yield return frame.ToBytes(FrameFormat);
                }
            }

            // A video without a single frame that can be read is of no more
            // use than one that cannot be opened.
            if (!sampledAny)
            {
                throw new RequestRefusedException(UnreadableVideo);
            }
        }

        /// <summary>
        /// How many frames apart the samples are: a second's worth, or more
        /// for a video that would otherwise give more than
        /// <see cref="MaxFrames"/>. Never less than one, which a video that
        /// gives no frame rate would otherwise get, and never move on.
        /// </summary>
        public static int FramesBetweenSamples(double framesPerSecond, int frameCount)
        {
            var oneSecond = framesPerSecond >= 1 ? (int)Math.Round(framesPerSecond) : AssumedFramesPerSecond;
            var spreadOverMax = (int)Math.Ceiling(frameCount / (double)MaxFrames);

            return Math.Max(oneSecond, spreadOverMax);
        }
    }
}
