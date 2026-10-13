namespace AnimalClassifier.Infrastructure.Imaging
{
    using System.Buffers.Binary;

    /// <summary>
    /// A WebP is a RIFF file whose first chunk gives the size, in one of three
    /// ways: the frame header of a lossy image, the header of a lossless one,
    /// or, for an extended one, such as an animation, the canvas every frame
    /// is drawn on. Only that first chunk is read, as the decoder does, and
    /// the decoder refuses an image whose frames do not fit it.
    /// </summary>
    internal static class WebPHeader
    {
        // The chunk's type, then its length, then its data.
        private const int ChunkTypeOffset = 12;
        private const int ChunkDataOffset = 20;

        // A lossy or lossless image gives its width and height in 14 bits each.
        private const int DimensionBits = 14;
        private const uint DimensionMask = (1 << DimensionBits) - 1;

        private static ReadOnlySpan<byte> LossyStartCode => [0x9D, 0x01, 0x2A];

        /// <returns>The size, or null when the image is not a WebP, or its first chunk gives none.</returns>
        public static ImageSize? ReadSize(ReadOnlySpan<byte> image)
        {
            if (image.Length < ChunkDataOffset || !image.StartsWith("RIFF"u8) || !image[8..].StartsWith("WEBP"u8))
            {
                return null;
            }

            var chunkType = image.Slice(ChunkTypeOffset, 4);
            var chunk = image[ChunkDataOffset..];

            if (chunkType.SequenceEqual("VP8 "u8))
            {
                return ReadLossySize(chunk);
            }

            if (chunkType.SequenceEqual("VP8L"u8))
            {
                return ReadLosslessSize(chunk);
            }

            return chunkType.SequenceEqual("VP8X"u8) ? ReadExtendedSize(chunk) : null;
        }

        // Three bytes of flags, a start code, then the width and the height,
        // each with two bits of scaling above it.
        private static ImageSize? ReadLossySize(ReadOnlySpan<byte> chunk)
        {
            if (chunk.Length < 10 || !chunk[3..].StartsWith(LossyStartCode))
            {
                return null;
            }

            return new ImageSize(
                BinaryPrimitives.ReadUInt16LittleEndian(chunk[6..]) & DimensionMask,
                BinaryPrimitives.ReadUInt16LittleEndian(chunk[8..]) & DimensionMask);
        }

        // A signature byte, then the width and the height less one, packed
        // from the lowest bit up.
        private static ImageSize? ReadLosslessSize(ReadOnlySpan<byte> chunk)
        {
            if (chunk.Length < 5 || chunk[0] != 0x2F)
            {
                return null;
            }

            var bits = BinaryPrimitives.ReadUInt32LittleEndian(chunk[1..]);

            return new ImageSize((bits & DimensionMask) + 1, ((bits >> DimensionBits) & DimensionMask) + 1);
        }

        // Flags and reserved bytes, then the width and the height of the
        // canvas less one, 24 bits each.
        private static ImageSize? ReadExtendedSize(ReadOnlySpan<byte> chunk)
        {
            if (chunk.Length < 10)
            {
                return null;
            }

            return new ImageSize(ReadUInt24LittleEndian(chunk[4..]) + 1, ReadUInt24LittleEndian(chunk[7..]) + 1);
        }

        private static uint ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes) =>
            bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16);
    }
}
