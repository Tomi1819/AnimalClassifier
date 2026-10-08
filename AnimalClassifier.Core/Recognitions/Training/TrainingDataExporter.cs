namespace AnimalClassifier.Core.Recognitions.Training
{
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using Microsoft.Extensions.Logging;
    using System.Globalization;
    using System.IO.Compression;
    using System.Text;

    public class TrainingDataExporter : ITrainingDataExporter
    {
        // Model Builder trains an image classifier from a folder holding a
        // folder of images per animal, named after it, so this one merges
        // into the folder the model was trained from.
        private const string KnownAnimalsFolder = "dataset/";

        // A new animal takes many more images to learn than a few users give,
        // so these are kept apart, to be added once there are enough.
        private const string UnlistedAnimalsFolder = "unlisted/";

        private const string ManifestEntryName = "manifest.csv";
        private const string ManifestHeader = "file,label,recognizedAnimal,predictionScore,verdict";
        private const char ManifestSeparator = ',';

        private readonly IRecognitionFeedbackRepository feedbackRepository;
        private readonly IFileStorageService fileStorage;
        private readonly ILogger<TrainingDataExporter> logger;

        public TrainingDataExporter(IRecognitionFeedbackRepository feedbackRepository,
                                    IFileStorageService fileStorage,
                                    ILogger<TrainingDataExporter> logger)
        {
            this.feedbackRepository = feedbackRepository;
            this.fileStorage = fileStorage;
            this.logger = logger;
        }

        // A caller who goes away stops it between images, as nothing is
        // written but the archive no one will read.
        public async Task<Stream> ExportAsync(CancellationToken cancellationToken)
        {
            var accepted = await feedbackRepository.GetAcceptedForTrainingAsync(cancellationToken);

            // On disk rather than in memory, since every accepted image goes
            // into it.
            return await TemporaryFile.WriteAsync(archive => WriteArchiveAsync(archive, accepted, cancellationToken));
        }

        private async Task WriteArchiveAsync(Stream destination, IReadOnlyList<RecognitionFeedback> accepted, CancellationToken cancellationToken)
        {
            await using var zip = await ZipArchive.CreateAsync(destination, ZipArchiveMode.Create,
                leaveOpen: true, entryNameEncoding: null, cancellationToken);

            var manifest = new StringBuilder().AppendLine(ManifestHeader);

            foreach (var feedback in accepted)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var image = fileStorage.GetPath(feedback.Recognition.UserId, feedback.Recognition.FileName);

                // Such as one removed by hand; the rest are still worth having.
                if (!File.Exists(image))
                {
                    logger.LogWarning("Left feedback {FeedbackId} out of the training data, as its image {Path} is missing.", feedback.Id, image);
                    continue;
                }

                var entryName = EntryName(feedback);

                // Images are compressed already, and squeezing them again
                // would cost time for nothing.
                await zip.CreateEntryFromFileAsync(image, entryName, CompressionLevel.NoCompression, cancellationToken);
                manifest.AppendLine(ManifestLine(entryName, feedback));
            }

            await using var manifestEntry = await zip.CreateEntry(ManifestEntryName).OpenAsync(cancellationToken);
            await using var writer = new StreamWriter(manifestEntry);
            await writer.WriteAsync(manifest, cancellationToken);
        }

        // Named after the feedback, which no other image shares, rather than
        // after the upload, which belongs to its user's folder.
        private static string EntryName(RecognitionFeedback feedback)
        {
            var folder = TrainingLabel.IsKnownToTheModel(feedback) ? KnownAnimalsFolder : UnlistedAnimalsFolder;

            return $"{folder}{TrainingLabel.For(feedback)}/{feedback.Id}{Path.GetExtension(feedback.Recognition.FileName)}";
        }

        // Every value is a number, or the name of an animal, which is either
        // the model's own or a name of letters that FeedbackValidator let
        // through, so none needs quoting or can start a spreadsheet formula.
        private static string ManifestLine(string entryName, RecognitionFeedback feedback) =>
            string.Join(ManifestSeparator,
                entryName,
                TrainingLabel.For(feedback),
                feedback.Recognition.AnimalName,
                feedback.Recognition.PredictionScore.ToString(CultureInfo.InvariantCulture),
                feedback.Verdict);
    }
}
