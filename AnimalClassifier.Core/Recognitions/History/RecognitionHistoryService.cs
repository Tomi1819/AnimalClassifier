namespace AnimalClassifier.Core.Recognitions.History
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Core.Recognitions.History.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using Microsoft.Extensions.Logging;
    using static AnimalClassifier.Core.Recognitions.History.HistoryMessages;

    public partial class RecognitionHistoryService : IRecognitionHistoryService
    {
        /// <summary>
        /// How many recognitions a page of the history holds.
        /// </summary>
        public const int PageSize = 20;

        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IFileStorageService fileStorage;
        private readonly IMediaLinkService mediaLinks;
        private readonly ILogger<RecognitionHistoryService> logger;

        public RecognitionHistoryService(IRecognitionLogRepository recognitionLogs,
                                         IFileStorageService fileStorage,
                                         IMediaLinkService mediaLinks,
                                         ILogger<RecognitionHistoryService> logger)
        {
            this.recognitionLogs = recognitionLogs;
            this.fileStorage = fileStorage;
            this.mediaLinks = mediaLinks;
            this.logger = logger;
        }

        public async Task<PagedResult<RecognitionHistoryItem>> GetHistoryAsync(string userId, int page, CancellationToken cancellationToken)
        {
            var (logs, totalCount) = await recognitionLogs.GetHistoryPageAsync(userId, page, PageSize, cancellationToken);

            return new PagedResult<RecognitionHistoryItem>
            {
                Items = logs.Select(ToHistoryItem).ToList(),
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount
            };
        }

        public async Task ClearHistoryAsync(string userId)
        {
            var cleared = await recognitionLogs.ClearHistoryAsync(userId);

            LogHistoryCleared(cleared, userId);
        }

        public async Task DeleteRecognitionAsync(string userId, int recognitionId)
        {
            var log = await recognitionLogs.GetForUserAsync(userId, recognitionId, CancellationToken.None)
                ?? throw new NotFoundException(RecognitionNotFound);

            await recognitionLogs.DeleteAsync(log.Id);

            // Only once the recognition is gone, since nothing could bring
            // the file back if removing the recognition failed.
            DeleteFile(userId, log.FileName);

            LogRecognitionDeleted(log.Id, userId);
        }

        // The recognition is gone by now, so a file that will not go, such as
        // one still being read, must not turn the answer into a failure. It
        // is logged for someone to remove by hand.
        private void DeleteFile(string userId, string fileName)
        {
            try
            {
                fileStorage.Delete(userId, fileName);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                LogFileNotDeleted(exception, fileName, userId);
            }
        }

        private RecognitionHistoryItem ToHistoryItem(AnimalRecognitionLog log) => new()
        {
            Id = log.Id,
            MediaPath = mediaLinks.CreateLink(log.UserId, log.FileName),
            RecognizedAnimal = log.AnimalName,
            DateRecognized = log.DateRecognized,
            PredictionScore = log.PredictionScore,
            FramesProcessed = log.FramesProcessed,
            IsVideo = !MediaFile.IsImage(log.FileName),
            Feedback = log.Feedback is null ? null : FeedbackDetails.From(log.Feedback)
        };

        [LoggerMessage(Level = LogLevel.Information, Message = "Cleared {Count} recognition(s) from the history of user {UserId}.")]
        private partial void LogHistoryCleared(int count, string userId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Deleted recognition {RecognitionId} of user {UserId}.")]
        private partial void LogRecognitionDeleted(int recognitionId, string userId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Could not delete {FileName} of user {UserId}, whose recognition was deleted.")]
        private partial void LogFileNotDeleted(Exception exception, string fileName, string userId);
    }
}
