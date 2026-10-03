namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Saves what the repositories were given, and makes several changes stand
    /// or fall together. Every repository and Identity's own stores share one
    /// database context per request, so this covers all of them at once.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Saves everything added to a repository since the last save.
        /// </summary>
        Task SaveChangesAsync();

        /// <summary>
        /// Runs the work in a transaction, which is committed once the work
        /// completes and rolled back if it throws.
        /// </summary>
        Task ExecuteInTransactionAsync(Func<Task> work);
    }
}
