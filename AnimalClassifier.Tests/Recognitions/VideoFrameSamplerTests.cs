namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Classification;
    using OpenCvSharp;

    public class VideoFrameSamplerTests : IDisposable
    {
        private readonly string directory = Directory.CreateTempSubdirectory("AnimalClassifierTests_").FullName;

        [Fact]
        public void AVideo_GivesOneFrameForEachSecond()
        {
            var video = WriteVideo(seconds: 3, framesPerSecond: 10);

            var frames = new VideoFrameSampler().SampleFrames(video).ToList();

            Assert.Equal(3, frames.Count);
        }

        [Fact]
        public void AFileThatIsNoVideo_IsRefused()
        {
            var notAVideo = Path.Combine(directory, "not-a-video.mp4");
            File.WriteAllBytes(notAVideo, [1, 2, 3, 4]);

            Assert.Throws<RequestRefusedException>(() => new VideoFrameSampler().SampleFrames(notAVideo).ToList());
        }

        public static TheoryData<double, int, int> Steps => new()
        {
            // A second's worth of frames.
            { 25, 250, 25 },
            { 29.97, 300, 30 },

            // No frame rate given, which would otherwise never move on.
            { 0, 300, VideoFrameSampler.AssumedFramesPerSecond },
            { double.NaN, 300, VideoFrameSampler.AssumedFramesPerSecond },

            // Ten minutes, spread over the most frames read from a video.
            { 30, 18_000, 18_000 / VideoFrameSampler.MaxFrames }
        };

        [Theory]
        [MemberData(nameof(Steps))]
        public void Samples_AreASecondApart_UnlessThereWouldBeTooMany(double framesPerSecond, int frameCount, int step)
        {
            Assert.Equal(step, VideoFrameSampler.FramesBetweenSamples(framesPerSecond, frameCount));
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);

        // Motion JPEG, which OpenCV writes and reads without a codec of the
        // system's own.
        private string WriteVideo(int seconds, int framesPerSecond)
        {
            var path = Path.Combine(directory, "video.avi");

            using (var writer = new VideoWriter(path, FourCC.MJPG, framesPerSecond, new Size(64, 64)))
            {
                using var frame = new Mat(64, 64, MatType.CV_8UC3, new Scalar(40, 120, 200));

                for (var i = 0; i < seconds * framesPerSecond; i++)
                {
                    writer.Write(frame);
                }
            }

            return path;
        }
    }
}
