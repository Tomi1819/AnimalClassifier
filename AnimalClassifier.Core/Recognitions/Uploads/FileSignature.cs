namespace AnimalClassifier.Core.Recognitions.Uploads
{
    /// <summary>
    /// What every file of a kind starts with: one or more runs of bytes, each
    /// at its own place from the start, as some formats name themselves a
    /// little way in.
    /// </summary>
    internal sealed class FileSignature
    {
        private readonly (int Offset, byte[] Bytes)[] parts;

        public FileSignature(params (int Offset, byte[] Bytes)[] parts)
        {
            this.parts = parts;
            Length = parts.Max(part => part.Offset + part.Bytes.Length);
        }

        /// <summary>
        /// How many of a file's first bytes it takes to tell whether it
        /// matches.
        /// </summary>
        public int Length { get; }

        /// <param name="start">The file's first bytes, or the whole of a file shorter than <see cref="Length"/>.</param>
        public bool Matches(ReadOnlySpan<byte> start)
        {
            foreach (var (offset, bytes) in parts)
            {
                if (start.Length < offset + bytes.Length || !start.Slice(offset, bytes.Length).SequenceEqual(bytes))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
