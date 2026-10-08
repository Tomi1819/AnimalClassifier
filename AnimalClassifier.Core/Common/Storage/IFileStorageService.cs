namespace AnimalClassifier.Core.Common.Storage
{
    /// <summary>
    /// The files users upload, kept in a folder per user. Nothing serves the
    /// folder, so a file reaches a browser only through whatever reads it
    /// from here.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Stores a user's file under a name of its own, which keeps the
        /// extension it was uploaded with.
        /// </summary>
        /// <param name="extension">The extension, with its dot, such as <c>.jpg</c>.</param>
        Task<StoredFile> SaveAsync(string userId, Stream content, string extension);

        /// <summary>
        /// Where one of a user's files is on disk, or null when it is not
        /// there, such as one removed by hand.
        /// </summary>
        string? FindPath(string userId, string fileName);

        /// <summary>
        /// Removes one stored file, such as one whose upload failed after it
        /// was stored. A file that is already gone is left as it is.
        /// </summary>
        void Delete(StoredFile file);

        /// <summary>
        /// The physical paths of every file one user has uploaded, or none for
        /// a user who has not uploaded any.
        /// </summary>
        IEnumerable<string> GetUserFiles(string userId);

        /// <summary>
        /// Removes every file one user has uploaded. A user with none is left
        /// as they are.
        /// </summary>
        void DeleteUserFiles(string userId);
    }
}
