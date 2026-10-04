namespace AnimalClassifier.Core.Common.Storage
{
    /// <summary>
    /// A file written on disk rather than in memory, for what can be too large
    /// to hold there, such as an archive of uploaded videos, and that is wanted
    /// only until it has been read.
    /// </summary>
    public static class TemporaryFile
    {
        /// <summary>
        /// Writes a new temporary file and hands it back from its start. The
        /// file deletes itself once the stream is closed, which for a file a
        /// controller answers with is after the response has been sent. One
        /// that fails to be written is deleted at once.
        /// </summary>
        /// <param name="write">Writes the file's contents to the stream it is given.</param>
        public static async Task<Stream> WriteAsync(Func<Stream, Task> write)
        {
            var file = new FileStream(Path.GetTempFileName(), new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.ReadWrite,
                Options = FileOptions.DeleteOnClose | FileOptions.Asynchronous
            });

            try
            {
                await write(file);
                file.Position = 0;

                return file;
            }
            catch
            {
                await file.DisposeAsync();
                throw;
            }
        }
    }
}
