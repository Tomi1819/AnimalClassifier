namespace AnimalClassifier.Infrastructure.Data.Repositories
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;

    public class RecognitionFeedbackRepository : IRecognitionFeedbackRepository
    {
        private readonly AnimalClassifierDbContext context;

        public RecognitionFeedbackRepository(AnimalClassifierDbContext context)
        {
            this.context = context;
        }

        public void Add(RecognitionFeedback feedback) => context.RecognitionFeedback.Add(feedback);

        public void Remove(RecognitionFeedback feedback) => context.RecognitionFeedback.Remove(feedback);

        public Task<RecognitionFeedback?> FindForUserAsync(string userId, int recognitionId) =>
            context.RecognitionFeedback
                .FirstOrDefaultAsync(f => f.RecognitionId == recognitionId && f.Recognition.UserId == userId);
    }
}
