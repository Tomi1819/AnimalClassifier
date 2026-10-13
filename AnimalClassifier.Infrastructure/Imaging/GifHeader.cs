namespace AnimalClassifier.Infrastructure.Imaging
{
    using System.Buffers.Binary;

    /// <summary>
    /// A GIF gives its size right after the signature, as the screen every
    /// frame is drawn on. The decoder refuses a frame that does not fit it,
    /// and reads only the first of an animation's.
    /// </summary>
    internal static class GifHeader
    {
        private const int WidthOffset = 6;
        private const int HeightOffset = 8;
        private const int HeaderLength = 10;

        // The two versions, from 1987 and 1989.
        private static ReadOnlySpan<byte> Signature87 => "GIF87a"u8;

        private static ReadOnlySpan<byte> Signature89 => "GIF89a"u8;

        /// <returns>The size, or null when the image is not a GIF.</returns>
        public static ImageSize? ReadSize(ReadOnlySpan<byte> image)
        {
            if (image.Length < HeaderLength || !(image.StartsWith(Signature87) || image.StartsWith(Signature89)))
            {
                return null;
            }

            return new ImageSize(
                BinaryPrimitives.ReadUInt16LittleEndian(image[WidthOffset..]),
                BinaryPrimitives.ReadUInt16LittleEndian(image[HeightOffset..]));
        }
    }
}
