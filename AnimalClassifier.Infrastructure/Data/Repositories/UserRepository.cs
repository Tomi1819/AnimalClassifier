namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using Microsoft.EntityFrameworkCore;

    public class UserRepository : IUserRepository
    {
        private readonly AnimalClassifierDbContext context;

        public UserRepository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public async Task<(IReadOnlyList<ApplicationUser> Users, int TotalCount)> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
        {
            var users = context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u => u.Email!.Contains(search) || u.FullName.Contains(search));
            }

            var totalCount = await users.CountAsync(cancellationToken);
            var pageOfUsers = await users
                .OrderByDescending(u => u.DateRegistered)
                .TakePage(page, pageSize)
                .ToListAsync(cancellationToken);

            return (pageOfUsers, totalCount);
        }
    }
}
