namespace AnimalClassifier.Core.Recognitions.History
{
    using AnimalClassifier.Core.Recognitions.History.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using Microsoft.Extensions.Logging;

    public class RecognitionHistoryService : IRecognitionHistoryService
    {
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IMediaLinkService mediaLinks;
        private readonly ILogger<RecognitionHistoryService> logger;

        public RecognitionHistoryService(IRecognitionLogRepository recognitionLogs,
                                         IMediaLinkService mediaLinks,
                                         ILogger<RecognitionHistoryService> logger)
        {
            this.recognitionLogs = recognitionLogs;
            this.mediaLinks = mediaLinks;
            this.logger = logger;
        }

        public async Task<IReadOnlyList<RecognitionHistoryItem>> GetHistoryAsync(string userId, CancellationToken cancellationToken)
        {
            var logs = await recognitionLogs.GetHistoryAsync(userId, cancellationToken);

            return logs.Select(ToHistoryItem).ToList();
        }

        public async Task ClearHistoryAsync(string userId)
        {
            var cleared = await recognitionLogs.ClearHistoryAsync(userId);

            logger.LogInformation("Cleared {Count} recognition(s) from the history of user {UserId}.", cleared, userId);
        }

        private RecognitionHistoryItem ToHistoryItem(AnimalRecognitionLog log) => new()
        {
            Id = log.Id,
            MediaPath = mediaLinks.CreateLink(log.UserId, log.FileName),
            RecognizedAnimal = log.AnimalName,
            DateRecognized = log.DateRecognized,
            PredictionScore = log.PredictionScore,
            FramesProcessed = log.FramesProcessed,
            IsVideo = !MediaFile.IsImage(log.FileName)
        };
    }
}
