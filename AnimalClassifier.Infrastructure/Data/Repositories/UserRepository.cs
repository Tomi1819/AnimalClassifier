namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;

    public class UserRepository : IUserRepository
    {
        private readonly AnimalClassifierDbContext context;

        public UserRepository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public async Task<(IReadOnlyList<ApplicationUser> Users, int TotalCount)> GetPageAsync(string? search, int page, int pageSize)
        {
            var users = context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u => u.Email!.Contains(search) || u.FullName.Contains(search));
            }

            var totalCount = await users.CountAsync();
            var pageOfUsers = await users
                .OrderByDescending(u => u.DateRegistered)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (pageOfUsers, totalCount);
        }
    }
}
