namespace AnimalClassifier.Infrastructure.Data.Common
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;
    public class Repository : IRepository
    {
        private readonly AnimalClassifierDbContext context;

        public Repository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public async Task AddRecognitionLogAsync(AnimalRecognitionLog animalRecognitionLog)
        {
            await context.AnimalRecognitionLogs.AddAsync(animalRecognitionLog);
        }

        public async Task<IEnumerable<AnimalRecognitionLog>> GetAllRecognitionLogsAsync()
        {
            return await context.AnimalRecognitionLogs.ToListAsync();
        }

        public async Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id)
        {
            return await context.AnimalRecognitionLogs.FindAsync(id);
        }

        public async Task<IEnumerable<AnimalRecognitionLog>> GetRecognitionLogsForUserAsync(string userId)
        {
            return await context.AnimalRecognitionLogs
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .OrderByDescending(l => l.DateRecognized)
                .ToListAsync();
        }

        public async Task<int> ClearRecognitionLogsForUserAsync(string userId)
        {
            var logs = await context.AnimalRecognitionLogs
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .ToListAsync();

            foreach (var log in logs)
            {
                log.IsDeleted = true;
            }

            await context.SaveChangesAsync();

            return logs.Count;
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
