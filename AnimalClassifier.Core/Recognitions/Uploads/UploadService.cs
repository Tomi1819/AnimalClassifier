namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Classification.Models;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Core.Recognitions.Uploads.Models;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System.Globalization;
    using static AnimalClassifier.Core.Recognitions.Uploads.UploadMessages;

    public class UploadService : IUploadService
    {
        /// <summary>
        /// What a video is recorded as when no animal shows clearly enough in
        /// it.
        /// </summary>
        public const string UnrecognisedAnimal = "Unknown";

        // The form the frontend reads a video's scores in.
        private const string ScoreFormat = "0.00";

        private readonly IImageClassifier classifier;
        private readonly IVideoFrameSampler frameSampler;
        private readonly IClassificationLimiter classificationLimiter;
        private readonly IFileStorageService fileStorage;
        private readonly IMediaLinkService mediaLinks;
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IUnitOfWork unitOfWork;
        private readonly ILogger<UploadService> logger;

        public UploadService(IImageClassifier classifier,
                             IVideoFrameSampler frameSampler,
                             IClassificationLimiter classificationLimiter,
                             IFileStorageService fileStorage,
                             IMediaLinkService mediaLinks,
                             IRecognitionLogRepository recognitionLogs,
                             IUnitOfWork unitOfWork,
                             ILogger<UploadService> logger)
        {
            this.classifier = classifier;
            this.frameSampler = frameSampler;
            this.classificationLimiter = classificationLimiter;
            this.fileStorage = fileStorage;
            this.mediaLinks = mediaLinks;
            this.recognitionLogs = recognitionLogs;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        public async Task<ImageUploadResult> UploadImageAsync(string userId, IFormFile file, CancellationToken cancellationToken)
        {
            UploadValidator.ValidateImage(file);

            var extension = Path.GetExtension(file.FileName);

            // Classified before it is stored, so that an image the model fails
            // on leaves nothing to remove.
            var (image, prediction) = await SanitizeAndClassifyAsync(await ReadAllBytesAsync(file), extension, cancellationToken);

            await using var content = new MemoryStream(image);
            var storedFile = await fileStorage.SaveAsync(userId, content, extension);

            var log = await RemovingOnFailureAsync(storedFile, () =>
                RecordAsync(userId, storedFile, prediction.Animal, prediction.Score, framesProcessed: null));

            return ToImageUploadResult(log);
        }

        public async Task<VideoUploadResult> UploadVideoAsync(string userId, IFormFile file, CancellationToken cancellationToken)
        {
            UploadValidator.ValidateVideo(file);

            var storedFile = await StoreAsync(userId, file);

            return await RemovingOnFailureAsync(storedFile, async () =>
            {
                // Read back from where it was stored, since a video's frames
                // are read from a file.
                var frames = await ClassifyFramesAsync(storedFile.PhysicalPath, cancellationToken);

                var topAnimals = VideoSummary.TopAnimals(frames);
                var strongest = topAnimals.FirstOrDefault();

                await RecordAsync(userId, storedFile, strongest?.Animal ?? UnrecognisedAnimal, strongest?.Score ?? 0, frames.Count);

                return new VideoUploadResult
                {
                    FramesProcessed = frames.Count,
                    TopAnimals = topAnimals.Select(ToAnimalSummary).ToList(),
                    VideoPath = mediaLinks.CreateLink(userId, storedFile.FileName)
                };
            });
        }

        public async Task<ImageUploadResult> GetImageUploadAsync(string userId, int recognitionId, CancellationToken cancellationToken)
        {
            var log = await recognitionLogs.GetForUserAsync(userId, recognitionId, cancellationToken)
                ?? throw new NotFoundException(RecognitionNotFound);

            return ToImageUploadResult(log);
        }

        // Decoding an image takes as much as classifying it, so it waits for
        // the same turn.
        private async Task<(byte[] Image, Prediction Prediction)> SanitizeAndClassifyAsync(byte[] upload, string extension, CancellationToken cancellationToken)
        {
            using var turn = await classificationLimiter.WaitTurnAsync(cancellationToken);

            var image = ImageSanitizer.Sanitize(upload, extension);

            return (image, classifier.Classify(image));
        }

        // A caller who goes away stops it between frames, as nothing has been
        // recorded yet.
        private async Task<List<Prediction>> ClassifyFramesAsync(string videoPath, CancellationToken cancellationToken)
        {
            using var turn = await classificationLimiter.WaitTurnAsync(cancellationToken);

            var frames = new List<Prediction>();

            foreach (var frame in frameSampler.SampleFrames(videoPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                frames.Add(classifier.Classify(frame));
            }

            return frames;
        }

        private async Task<StoredFile> StoreAsync(string userId, IFormFile file)
        {
            await using var content = file.OpenReadStream();

            return await fileStorage.SaveAsync(userId, content, Path.GetExtension(file.FileName));
        }

        private async Task<AnimalRecognitionLog> RecordAsync(string userId, StoredFile storedFile, string animal, float score, int? framesProcessed)
        {
            var log = new AnimalRecognitionLog
            {
                UserId = userId,
                FileName = storedFile.FileName,
                AnimalName = animal,
                PredictionScore = score,
                FramesProcessed = framesProcessed,
                DateRecognized = DateTime.UtcNow
            };

            recognitionLogs.Add(log);
            await unitOfWork.SaveChangesAsync();

            return log;
        }

        // A file without a recognition would be nobody's to see or delete, so
        // one whose recognition fails is removed again.
        private async Task<T> RemovingOnFailureAsync<T>(StoredFile storedFile, Func<Task<T>> work)
        {
            try
            {
                return await work();
            }
            catch
            {
                Remove(storedFile);
                throw;
            }
        }

        // Failing to remove it must not hide why the upload failed, so it is
        // logged for someone to remove by hand.
        private void Remove(StoredFile storedFile)
        {
            try
            {
                fileStorage.Delete(storedFile);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "Could not remove {Path}, stored for an upload that failed.", storedFile.PhysicalPath);
            }
        }

        private static async Task<byte[]> ReadAllBytesAsync(IFormFile file)
        {
            await using var content = file.OpenReadStream();
            using var buffer = new MemoryStream();

            await content.CopyToAsync(buffer);

            return buffer.ToArray();
        }

        private ImageUploadResult ToImageUploadResult(AnimalRecognitionLog log) => new()
        {
            ImageId = log.Id,
            ImagePath = mediaLinks.CreateLink(log.UserId, log.FileName),
            RecognizedAnimal = log.AnimalName,
            DateRecognized = log.DateRecognized,
            PredictionScore = log.PredictionScore
        };

        private static AnimalSummary ToAnimalSummary(Prediction animal) => new()
        {
            Animal = animal.Animal,
            AverageScore = animal.Score.ToString(ScoreFormat, CultureInfo.InvariantCulture)
        };
    }
}
