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
        private const int TypeOffset = 8;
        private const int ChunkTypeOffset = 12;
        private const int ChunkTypeLength = 4;

        // After the chunk's type and its length.
        private const int ChunkDataOffset = 20;

        // A lossy frame header starts with three bytes of flags and a start
        // code, followed by the width and the height, 14 bits each, with two
        // bits of scaling above them.
        private const int LossyStartCodeOffset = 3;
        private const int LossyWidthOffset = 6;
        private const int LossyHeightOffset = 8;
        private const int LossyHeaderLength = 10;
        private const ushort LossyDimensionMask = 0x3FFF;

        // A lossless header is a signature byte, then the width and the height
        // less one, 14 bits each, packed from the lowest bit up.
        private const byte LosslessSignature = 0x2F;
        private const int LosslessSizeOffset = 1;
        private const int LosslessHeaderLength = 5;
        private const int LosslessDimensionBits = 14;
        private const uint LosslessDimensionMask = 0x3FFF;

        // An extended header is flags and reserved bytes, then the width and
        // the height of the canvas less one, 24 bits each.
        private const int ExtendedWidthOffset = 4;
        private const int ExtendedHeightOffset = 7;
        private const int ExtendedHeaderLength = 10;

        private static ReadOnlySpan<byte> RiffSignature => "RIFF"u8;

        private static ReadOnlySpan<byte> WebPType => "WEBP"u8;

        private static ReadOnlySpan<byte> LossyChunkType => "VP8 "u8;

        private static ReadOnlySpan<byte> LosslessChunkType => "VP8L"u8;

        private static ReadOnlySpan<byte> ExtendedChunkType => "VP8X"u8;

        private static ReadOnlySpan<byte> LossyStartCode => [0x9D, 0x01, 0x2A];

        /// <returns>The size, or null when the image is not a WebP, or its first chunk gives none.</returns>
        public static ImageSize? ReadSize(ReadOnlySpan<byte> image)
        {
            if (image.Length < ChunkDataOffset
                || !image.StartsWith(RiffSignature)
                || !image[TypeOffset..].StartsWith(WebPType))
            {
                return null;
            }

            var chunkType = image.Slice(ChunkTypeOffset, ChunkTypeLength);
            var chunk = image[ChunkDataOffset..];

            if (chunkType.SequenceEqual(LossyChunkType))
            {
                return ReadLossySize(chunk);
            }

            if (chunkType.SequenceEqual(LosslessChunkType))
            {
                return ReadLosslessSize(chunk);
            }

            return chunkType.SequenceEqual(ExtendedChunkType) ? ReadExtendedSize(chunk) : null;
        }

        private static ImageSize? ReadLossySize(ReadOnlySpan<byte> chunk)
        {
            if (chunk.Length < LossyHeaderLength || !chunk[LossyStartCodeOffset..].StartsWith(LossyStartCode))
            {
                return null;
            }

            return new ImageSize(
                (uint)(BinaryPrimitives.ReadUInt16LittleEndian(chunk[LossyWidthOffset..]) & LossyDimensionMask),
                (uint)(BinaryPrimitives.ReadUInt16LittleEndian(chunk[LossyHeightOffset..]) & LossyDimensionMask));
        }

        private static ImageSize? ReadLosslessSize(ReadOnlySpan<byte> chunk)
        {
            if (chunk.Length < LosslessHeaderLength || chunk[0] != LosslessSignature)
            {
                return null;
            }

            var bits = BinaryPrimitives.ReadUInt32LittleEndian(chunk[LosslessSizeOffset..]);

            return new ImageSize(
                (bits & LosslessDimensionMask) + 1,
                ((bits >> LosslessDimensionBits) & LosslessDimensionMask) + 1);
        }

        private static ImageSize? ReadExtendedSize(ReadOnlySpan<byte> chunk)
        {
            if (chunk.Length < ExtendedHeaderLength)
            {
                return null;
            }

            return new ImageSize(
                ReadUInt24LittleEndian(chunk[ExtendedWidthOffset..]) + 1,
                ReadUInt24LittleEndian(chunk[ExtendedHeightOffset..]) + 1);
        }

        private static uint ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes) =>
            bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16);
    }
}
