namespace AnimalClassifier.Core.Recognitions.Media
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Storage;
    using Microsoft.AspNetCore.DataProtection;
    using System.Security.Cryptography;
    using static AnimalClassifier.Core.Recognitions.Media.MediaMessages;

    public class MediaLinkService : IMediaLinkService
    {
        /// <summary>
        /// Where the links lead, which the controller answering them is routed by.
        /// </summary>
        public const string Route = "api/media";

        /// <summary>
        /// How long a link works. A page is answered with fresh ones each time
        /// it asks, so this only has to outlast the page being looked at.
        /// </summary>
        public const int LifetimeMinutes = 60;

        // Versioned, so that a change to what a link holds can be made
        // without one in flight being read under the old meaning.
        private const string Purpose = "AnimalClassifier.Media.v1";

        // Neither a user id nor a stored file's name contains it.
        private const char Separator = '/';

        private readonly ITimeLimitedDataProtector protector;
        private readonly IFileStorageService fileStorage;

        public MediaLinkService(IDataProtectionProvider dataProtectionProvider, IFileStorageService fileStorage)
        {
            protector = dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
            this.fileStorage = fileStorage;
        }

        public string CreateLink(string userId, string fileName) =>
            $"/{Route}/{protector.Protect($"{userId}{Separator}{fileName}", TimeSpan.FromMinutes(LifetimeMinutes))}";

        public string GetPath(string token)
        {
            string userAndFile;

            try
            {
                userAndFile = protector.Unprotect(token);
            }
            catch (CryptographicException)
            {
                throw new NotFoundException(MediaNotFound);
            }

            var separator = userAndFile.IndexOf(Separator);
            var path = fileStorage.GetPath(userAndFile[..separator], userAndFile[(separator + 1)..]);

            return File.Exists(path) ? path : throw new NotFoundException(MediaNotFound);
        }
    }
}
