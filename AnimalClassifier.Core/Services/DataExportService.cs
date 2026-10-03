namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Identity.Passkeys;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
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
        private readonly IRepository repository;
        private readonly IFileStorageService fileStorageService;

        public DataExportService(IAccountService accountService,
                                 IPasskeyService passkeyService,
                                 IRepository repository,
                                 IFileStorageService fileStorageService)
        {
            this.accountService = accountService;
            this.passkeyService = passkeyService;
            this.repository = repository;
            this.fileStorageService = fileStorageService;
        }

        public async Task<Stream> ExportAsync(string userId)
        {
            // Everything is read before the archive is started, so that an
            // account that does not exist fails before any file is made.
            var account = await ReadAccountAsync(userId);
            var recognitions = await ReadRecognitionsAsync(userId);
            var files = fileStorageService.GetUserFiles(userId);

            var archive = CreateTemporaryFile();

            try
            {
                await WriteArchiveAsync(archive, account, recognitions, files);
                archive.Position = 0;

                return archive;
            }
            catch
            {
                await archive.DisposeAsync();
                throw;
            }
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
            var logs = await repository.GetAllRecognitionLogsForUserAsync(userId);

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

        // On disk rather than in memory, since uploaded videos can make the
        // archive large. The file deletes itself once the stream is closed,
        // which is after the response has been sent.
        private static FileStream CreateTemporaryFile() =>
            new(Path.GetTempFileName(), new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.ReadWrite,
                Options = FileOptions.DeleteOnClose | FileOptions.Asynchronous
            });

        private static ExportedRecognition ToExportedRecognition(AnimalRecognitionLog log) => new()
        {
            RecognizedAnimal = log.AnimalName,
            PredictionScore = log.PredictionScore,
            FramesProcessed = log.FramesProcessed,
            DateRecognized = log.DateRecognized,
            IsCleared = log.IsDeleted,
            File = ToArchivePath(log.ImagePath)
        };

        // Both a recognition's public path and an upload's physical path end
        // in the stored file's name, so the two meet at the same entry.
        private static string ToArchivePath(string path) => UploadsFolder + Path.GetFileName(path);
    }
}
