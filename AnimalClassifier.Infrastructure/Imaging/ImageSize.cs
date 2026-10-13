namespace AnimalClassifier.Infrastructure.Imaging
{
    /// <summary>
    /// The size an image's header gives it, which is what a decoder allocates
    /// for, whatever the rest of the file holds.
    /// </summary>
    internal readonly record struct ImageSize(uint Width, uint Height)
    {
        // Unsigned, as the product of two of a PNG's sizes overflows a long.
        public ulong Pixels => (ulong)Width * Height;
    }
}
