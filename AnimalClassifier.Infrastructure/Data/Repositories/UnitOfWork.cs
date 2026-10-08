namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Core.Data.Repositories;

    public class UnitOfWork : IUnitOfWork
    {
        private readonly AnimalClassifierDbContext context;

        public UnitOfWork(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public Task SaveChangesAsync() => context.SaveChangesAsync();

        // Kept in one place, so that retrying on a transient failure, should
        // the connection ever be set up for it, is a change made here alone.
        public async Task ExecuteInTransactionAsync(Func<Task> work)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            await work();

            await transaction.CommitAsync();
        }
    }
}
