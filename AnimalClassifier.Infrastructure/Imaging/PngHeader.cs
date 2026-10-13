namespace AnimalClassifier.Infrastructure.Imaging
{
    using System.Buffers.Binary;

    /// <summary>
    /// A PNG keeps its size in the IHDR chunk, right after the signature.
    /// </summary>
    internal static class PngHeader
    {
        private const int ChunkTypeOffset = 12;
        private const int WidthOffset = 16;
        private const int HeightOffset = 20;
        private const int HeaderLength = 24;

        private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        private static ReadOnlySpan<byte> HeaderChunkType => "IHDR"u8;

        /// <returns>The size, or null when the image is not a PNG.</returns>
        public static ImageSize? ReadSize(ReadOnlySpan<byte> image)
        {
            if (image.Length < HeaderLength
                || !image.StartsWith(Signature)
                || !image[ChunkTypeOffset..].StartsWith(HeaderChunkType))
            {
                return null;
            }

            return new ImageSize(
                BinaryPrimitives.ReadUInt32BigEndian(image[WidthOffset..]),
                BinaryPrimitives.ReadUInt32BigEndian(image[HeightOffset..]));
        }
    }
}
