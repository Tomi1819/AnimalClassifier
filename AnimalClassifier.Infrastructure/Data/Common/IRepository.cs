namespace AnimalClassifier.Infrastructure.Data.Common
{
    using AnimalClassifier.Infrastructure.Data.Models;
    public interface IRepository
    {
        Task<IEnumerable<AnimalRecognitionLog>> GetAllRecognitionLogsAsync();
        Task<AnimalRecognitionLog> GetRecognitionLogByIdAsync(int id);
        Task AddRecognitionLogAsync(AnimalRecognitionLog animalRecognitionLog);
        Task SaveChangesAsync();
    }
}
