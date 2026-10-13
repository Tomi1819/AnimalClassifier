namespace AnimalClassifier.Infrastructure.Imaging
{
    using System.Buffers.Binary;

    /// <summary>
    /// A JPEG is a run of segments, each a marker and most of them a length,
    /// and the size is in the frame header, which can follow any number of
    /// others, such as the metadata. Anything a decoder would pass over rather
    /// than read, such as stray bytes between segments, is refused instead, so
    /// that what is read here is never a different header from the one the
    /// decoder goes on to read.
    /// </summary>
    internal static class JpegHeader
    {
        private const byte MarkerPrefix = 0xFF;
        private const byte StartOfImage = 0xD8;
        private const byte StartOfScan = 0xDA;
        private const byte EndOfImage = 0xD9;
        private const byte Temporary = 0x01;
        private const byte FirstRestart = 0xD0;
        private const byte LastRestart = 0xD7;

        /// <returns>The size, or null when the image is not a JPEG, or its frame header cannot be found.</returns>
        public static ImageSize? ReadSize(ReadOnlySpan<byte> image)
        {
            if (image.Length < 2 || image[0] != MarkerPrefix || image[1] != StartOfImage)
            {
                return null;
            }

            var position = 2;

            while (position + 4 <= image.Length && image[position] == MarkerPrefix)
            {
                var marker = image[position + 1];

                // Padding before the marker itself.
                if (marker == MarkerPrefix)
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
                    return null;
                }

                if (IsStartOfFrame(marker))
                {
                    // Its length, its precision, then the height and the width.
                    if (position + 7 > image.Length)
                    {
                        return null;
                    }

                    return new ImageSize(
                        Width: BinaryPrimitives.ReadUInt16BigEndian(image[(position + 5)..]),
                        Height: BinaryPrimitives.ReadUInt16BigEndian(image[(position + 3)..]));
                }

                position += length;
            }

            return null;
        }

        // TEM and the restart markers, which carry no length.
        private static bool IsStandalone(byte marker) =>
            marker is Temporary or (>= FirstRestart and <= LastRestart);

        // Every marker from C0 up carries a length but these: a second start,
        // the end, and the start of the image data, which a frame header has
        // to come before.
        private static bool IsSegment(byte marker) =>
            marker is >= 0xC0 and < MarkerPrefix and not StartOfImage and not EndOfImage and not StartOfScan;

        // C4, C8 and CC share the range but hold tables rather than a frame.
        private static bool IsStartOfFrame(byte marker) =>
            marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;
    }
}
