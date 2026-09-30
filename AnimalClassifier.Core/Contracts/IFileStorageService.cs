namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    using Microsoft.AspNetCore.Http;

    public interface IFileStorageService
    {
        Task<StoredFileResult> SaveFileAsync(IFormFile file, string userId);

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
