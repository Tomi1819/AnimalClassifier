namespace AnimalClassifier.Infrastructure.Imaging
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions;
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

        public byte[] Sanitize(byte[] image)
        {
            var (size, canBeTransparent) = ReadHeader(image) ?? throw new RequestRefusedException(UnreadableImage);

            if (size.Pixels > MaxMegapixels * PixelsPerMegapixel)
            {
                throw new RequestRefusedException(string.Format(CultureInfo.InvariantCulture, ImageTooLarge, MaxMegapixels));
            }

            using var decoded = canBeTransparent ? DecodeOverWhite(image) : Decode(image);

            if (decoded.Empty())
            {
                throw new RequestRefusedException(UnreadableImage);
            }

            return decoded.ToBytes(MediaFile.StoredImageExtension, new ImageEncodingParam(ImwriteFlags.JpegQuality, JpegQuality));
        }

        // Each format is told by its signature, which is also how the decoder
        // is chosen, so the header read is the one that decoder reads. A JPEG
        // is never see-through, and each of the others can be.
        private static (ImageSize Size, bool CanBeTransparent)? ReadHeader(ReadOnlySpan<byte> image)
        {
            if (JpegHeader.ReadSize(image) is { } jpegSize)
            {
                return (jpegSize, false);
            }

            var size = PngHeader.ReadSize(image)
                ?? WebPHeader.ReadSize(image)
                ?? GifHeader.ReadSize(image);

            return size is null ? null : (size.Value, true);
        }

        // In 8-bit blue, green and red, turned the way the file says the image
        // is to be seen.
        private static Mat Decode(byte[] image) => Cv2.ImDecode(image, ImreadModes.Color);

        // Decoded as it is first, which is the only way its transparency is
        // kept, though not the way it is to be turned. An image that has some
        // is a drawing or a cut-out rather than a photo, so it is laid over
        // white as it is; one that has none is decoded again as a photo is.
        private static Mat DecodeOverWhite(byte[] image)
        {
            using (var unchanged = Cv2.ImDecode(image, ImreadModes.Unchanged))
            {
                if (unchanged.Channels() == Transparency.ChannelsWithAlpha)
                {
                    return Transparency.LayOverWhite(unchanged);
                }
            }

            return Decode(image);
        }
    }
}
