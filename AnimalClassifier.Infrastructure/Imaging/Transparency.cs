namespace AnimalClassifier.Infrastructure.Imaging
{
    using OpenCvSharp;

    /// <summary>
    /// A JPEG has no transparency, so an image that has some is laid over
    /// white first, as a page would show it. Left as it is, each see-through
    /// pixel keeps whatever colour the file gives it underneath, which is most
    /// often black.
    /// </summary>
    internal static class Transparency
    {
        /// <summary>
        /// What OpenCV decodes an image with transparency to, unchanged: blue,
        /// green, red and how opaque, in that order.
        /// </summary>
        public const int ChannelsWithAlpha = 4;

        private const int AlphaChannel = 3;

        // The most a 16-bit channel holds, which a PNG can have, over the most
        // an 8-bit one does.
        private const double SixteenToEightBit = 255.0 / ushort.MaxValue;

        private static readonly Scalar White = Scalar.All(byte.MaxValue);

        /// <param name="image">An image of <see cref="ChannelsWithAlpha"/> channels, of 8 or 16 bits each.</param>
        /// <returns>The image as it looks over white, in 8-bit blue, green and red.</returns>
        public static Mat LayOverWhite(Mat image)
        {
            using var eightBit = new Mat();
            image.ConvertTo(eightBit, MatType.CV_8UC4, image.Depth() == MatType.CV_16U ? SixteenToEightBit : 1);

            using var colour = eightBit.CvtColor(ColorConversionCodes.BGRA2BGR);
            using var white = new Mat(colour.Size(), MatType.CV_8UC3, White);

            using var alpha = eightBit.ExtractChannel(AlphaChannel);
            using var opacity = new Mat();
            alpha.ConvertTo(opacity, MatType.CV_32FC1, 1.0 / byte.MaxValue);
            using var seeThrough = new Mat();
            Cv2.Subtract(Scalar.All(1), opacity, seeThrough);

            var laidOver = new Mat();
            Cv2.BlendLinear(colour, white, opacity, seeThrough, laidOver);

            return laidOver;
        }
    }
}
