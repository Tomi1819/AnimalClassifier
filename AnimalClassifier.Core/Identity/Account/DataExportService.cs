namespace AnimalClassifier.Core.Identity.Account
{
    using AnimalClassifier.Core.Common.Storage;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Data.Repositories;
    using AnimalClassifier.Core.Identity.Account.Models;
    using AnimalClassifier.Core.Identity.Passkeys;
    using System.IO.Compression;
    using System.Text.Json;

    public class DataExportService : IDataExportService
    {
        private const string AccountEntryName = "account.json";
        private const string RecognitionsEntryName = "recognitions.json";
        private const string UploadsFolder = "uploads/";

        // Indented, since the copy is meant for a person to read as well as a
        // program.
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web) { WriteIndented = true };

        private readonly IAccountService accountService;
        private readonly IPasskeyService passkeyService;
        private readonly IRecognitionLogRepository recognitionLogs;
        private readonly IFileStorageService fileStorageService;

        public DataExportService(IAccountService accountService,
                                 IPasskeyService passkeyService,
                                 IRecognitionLogRepository recognitionLogs,
                                 IFileStorageService fileStorageService)
        {
            this.accountService = accountService;
            this.passkeyService = passkeyService;
            this.recognitionLogs = recognitionLogs;
            this.fileStorageService = fileStorageService;
        }

        public async Task<Stream> ExportAsync(string userId)
        {
            // Everything is read before the archive is started, so that an
            // account that does not exist fails before any file is made.
            var account = await ReadAccountAsync(userId);
            var recognitions = await ReadRecognitionsAsync(userId);
            var files = fileStorageService.GetUserFiles(userId);

            // On disk rather than in memory, since uploaded videos can make
            // the archive large.
            return await TemporaryFile.WriteAsync(archive => WriteArchiveAsync(archive, account, recognitions, files));
        }

        private async Task<ExportedAccount> ReadAccountAsync(string userId)
        {
            var profile = await accountService.GetProfileAsync(userId);
            var passkeys = await passkeyService.GetPasskeysAsync(userId);

            return new ExportedAccount
            {
                FullName = profile.FullName,
                Email = profile.Email,
                DateRegistered = profile.DateRegistered,
                Passkeys = passkeys
                    .Select(passkey => new ExportedPasskey { Name = passkey.Name, DateAdded = passkey.DateAdded })
                    .ToList()
            };
        }

        private async Task<IEnumerable<ExportedRecognition>> ReadRecognitionsAsync(string userId)
        {
            var logs = await recognitionLogs.GetAllForUserAsync(userId);

            return logs.Select(ToExportedRecognition).ToList();
        }

        private static async Task WriteArchiveAsync(Stream destination,
                                                    ExportedAccount account,
                                                    IEnumerable<ExportedRecognition> recognitions,
                                                    IEnumerable<string> files)
        {
            await using var zip = await ZipArchive.CreateAsync(destination, ZipArchiveMode.Create,
                leaveOpen: true, entryNameEncoding: null);

            await WriteJsonEntryAsync(zip, AccountEntryName, account);
            await WriteJsonEntryAsync(zip, RecognitionsEntryName, recognitions);

            foreach (var file in files)
            {
                // Images and videos are compressed already, and squeezing them
                // again would cost time for nothing.
                await zip.CreateEntryFromFileAsync(file, ToArchivePath(file), CompressionLevel.NoCompression);
            }
        }

        private static async Task WriteJsonEntryAsync<T>(ZipArchive zip, string entryName, T value)
        {
            await using var entry = await zip.CreateEntry(entryName).OpenAsync();

            await JsonSerializer.SerializeAsync(entry, value, JsonOptions);
        }

        private static ExportedRecognition ToExportedRecognition(AnimalRecognitionLog log) => new()
        {
            RecognizedAnimal = log.AnimalName,
            PredictionScore = log.PredictionScore,
            FramesProcessed = log.FramesProcessed,
            DateRecognized = log.DateRecognized,
            IsCleared = log.IsDeleted,
            File = ToArchivePath(log.FileName),
            Feedback = log.Feedback is null ? null : ToExportedFeedback(log.Feedback)
        };

        private static ExportedFeedback ToExportedFeedback(RecognitionFeedback feedback) => new()
        {
            Verdict = feedback.Verdict,
            ActualAnimal = feedback.ActualAnimal,
            Comment = feedback.Comment,
            AllowsTraining = feedback.AllowsTraining,
            ReviewStatus = feedback.ReviewStatus,
            DateSubmitted = feedback.DateSubmitted
        };

        // A recognition names its file, and an upload's physical path ends in
        // that name, so the two meet at the same entry.
        private static string ToArchivePath(string path) => UploadsFolder + Path.GetFileName(path);
    }
}
