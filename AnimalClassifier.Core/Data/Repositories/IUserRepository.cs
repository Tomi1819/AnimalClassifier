namespace AnimalClassifier.Core.Data.Repositories
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// Reads users in the ways Identity's user manager does not offer. Every
    /// change to a user still goes through the user manager, which keeps its
    /// security stamp and normalised fields in step.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// One page of the users whose email or name contains the search term,
        /// newest first, with the number of users that match.
        /// </summary>
        Task<(IReadOnlyList<ApplicationUser> Users, int TotalCount)> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    }
}
