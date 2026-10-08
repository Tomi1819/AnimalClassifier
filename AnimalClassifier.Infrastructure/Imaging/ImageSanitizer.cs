namespace AnimalClassifier.Infrastructure.Imaging
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Uploads;
    using OpenCvSharp;
    using System.Buffers.Binary;
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

        // Where a PNG keeps its size: the IHDR chunk, right after the signature.
        private const int PngChunkTypeOffset = 12;
        private const int PngWidthOffset = 16;
        private const int PngHeaderLength = 24;

        private const byte JpegMarkerPrefix = 0xFF;
        private const byte JpegStartOfImage = 0xD8;
        private const byte JpegStartOfScan = 0xDA;
        private const byte JpegEndOfImage = 0xD9;
        private const byte JpegTemporary = 0x01;
        private const byte JpegFirstRestart = 0xD0;
        private const byte JpegLastRestart = 0xD7;

        private static readonly byte[] PngHeaderChunkType = "IHDR"u8.ToArray();

        public byte[] Sanitize(byte[] image, string extension)
        {
            if (!TryReadPngSize(image, out var width, out var height) && !TryReadJpegSize(image, out width, out height))
            {
                throw new RequestRefusedException(UnreadableImage);
            }

            // Unsigned, as the product of two of a PNG's sizes overflows a long.
            if ((ulong)width * height > MaxMegapixels * PixelsPerMegapixel)
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

        private static bool TryReadPngSize(ReadOnlySpan<byte> image, out uint width, out uint height)
        {
            width = height = 0;

            if (image.Length < PngHeaderLength
                || !image.Slice(PngChunkTypeOffset, PngHeaderChunkType.Length).SequenceEqual(PngHeaderChunkType))
            {
                return false;
            }

            width = BinaryPrimitives.ReadUInt32BigEndian(image[PngWidthOffset..]);
            height = BinaryPrimitives.ReadUInt32BigEndian(image[(PngWidthOffset + 4)..]);

            return true;
        }

        // A JPEG is a run of segments, each a marker and most of them a
        // length, and the size is in the frame header, which can follow any
        // number of others, such as the metadata. Anything a decoder would
        // pass over rather than read, such as stray bytes between segments,
        // is refused instead, so that what is read here is never a different
        // header from the one the decoder goes on to read.
        private static bool TryReadJpegSize(ReadOnlySpan<byte> image, out uint width, out uint height)
        {
            width = height = 0;

            if (image.Length < 2 || image[0] != JpegMarkerPrefix || image[1] != JpegStartOfImage)
            {
                return false;
            }

            var position = 2;

            while (position + 4 <= image.Length && image[position] == JpegMarkerPrefix)
            {
                var marker = image[position + 1];

                // Padding before the marker itself.
                if (marker == JpegMarkerPrefix)
                {
                    position++;
                    continue;
                }

                position += 2;

                if (IsStandalone(marker))
                {
                    continue;
                }

                var length = BinaryPrimitives.ReadUInt16BigEndian(image[position..]);

                if (!IsSegment(marker) || length < 2)
                {
                    return false;
                }

                if (IsStartOfFrame(marker))
                {
                    // Its length, its precision, then the height and the width.
                    if (position + 7 > image.Length)
                    {
                        return false;
                    }

                    height = BinaryPrimitives.ReadUInt16BigEndian(image[(position + 3)..]);
                    width = BinaryPrimitives.ReadUInt16BigEndian(image[(position + 5)..]);

                    return true;
                }

                position += length;
            }

            return false;
        }

        // TEM and the restart markers, which carry no length.
        private static bool IsStandalone(byte marker) =>
            marker is JpegTemporary or (>= JpegFirstRestart and <= JpegLastRestart);

        // Every marker from C0 up carries a length but these: a second start,
        // the end, and the start of the image data, which a frame header has
        // to come before.
        private static bool IsSegment(byte marker) =>
            marker is >= 0xC0 and < JpegMarkerPrefix and not JpegStartOfImage and not JpegEndOfImage and not JpegStartOfScan;

        // C4, C8 and CC share the range but hold tables rather than a frame.
        private static bool IsStartOfFrame(byte marker) =>
            marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;
    }
}
