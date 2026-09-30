namespace AnimalClassifier.Core.Contracts
{
    /// <summary>
    /// A copy of everything an account holds, for its owner to keep.
    /// </summary>
    public interface IDataExportService
    {
        /// <summary>
        /// Builds a ZIP archive of the account: its profile and passkeys in
        /// <c>account.json</c>, every recognition in <c>recognitions.json</c>,
        /// cleared ones included and marked as such, and the uploaded files
        /// under <c>uploads/</c>.
        ///
        /// What only serves signing in, such as the password hash and the
        /// passkeys' credentials, is left out.
        /// </summary>
        /// <returns>
        /// The archive, read from its start. It is kept in a temporary file,
        /// which is deleted once the stream is closed.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// When the account does not exist.
        /// </exception>
        Task<Stream> ExportAsync(string userId);
    }
}
