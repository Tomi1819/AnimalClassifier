namespace AnimalClassifier.Infrastructure.Imaging
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Uploads;
    using OpenCvSharp;
    using System.Globalization;
    using static AnimalClassifier.Core.Recognitions.Uploads.UploadMessages;

    /// <summary>
    /// Decodes and encodes with OpenCV. The size is read from the header
    /// first, since decoding allocates the whole image at once, and a file of
    /// a few megabytes can claim billions of pixels.
    /// </summary>
    public class ImageSanitizer : IImageSanitizer
    {
        /// <summary>
        /// The most pixels an image can have, in millions. More than any phone
        /// camera takes in a file small enough to upload.
        /// </summary>
        public const int MaxMegapixels = 50;

        private const ulong PixelsPerMegapixel = 1_000_000;

        private const int JpegQuality = 95;

        public byte[] Sanitize(byte[] image, string extension)
        {
            var size = ReadSize(image) ?? throw new RequestRefusedException(UnreadableImage);

            if (size.Pixels > MaxMegapixels * PixelsPerMegapixel)
            {
                throw new RequestRefusedException(string.Format(CultureInfo.InvariantCulture, ImageTooLarge, MaxMegapixels));
            }

            using var decoded = Cv2.ImDecode(image, ImreadModes.Color);

            if (decoded.Empty())
            {
                throw new RequestRefusedException(UnreadableImage);
            }

            return decoded.ToBytes(extension.ToLowerInvariant(), new ImageEncodingParam(ImwriteFlags.JpegQuality, JpegQuality));
        }

        // Each format is told by its signature, which is also how the decoder
        // is chosen, so the header read is the one that decoder reads.
        private static ImageSize? ReadSize(ReadOnlySpan<byte> image) =>
            PngHeader.ReadSize(image) ?? JpegHeader.ReadSize(image);
    }
}
