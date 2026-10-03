namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Common.Exceptions;
    using Microsoft.AspNetCore.Http;
    using static AnimalClassifier.Core.Recognitions.Uploads.UploadMessages;

    /// <summary>
    /// What an uploaded file has to be for a recognition to be made from it.
    ///
    /// Its name and content type are whatever the caller says they are, so an
    /// image's first bytes are checked as well. Those are what the model
    /// reads, and anything that is not really an image would fail inside it.
    /// </summary>
    public static class UploadValidator
    {
        /// <summary>
        /// The largest file accepted, image or video, in bytes.
        /// </summary>
        public const long MaxFileSize = 5 * BytesPerMegabyte;

        private const long BytesPerMegabyte = 1024 * 1024;

        private static readonly IReadOnlySet<string> ImageContentTypes =
            new HashSet<string>(["image/jpeg", "image/png"], StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlySet<string> VideoContentTypes =
            new HashSet<string>(["video/mp4", "video/quicktime", "video/x-msvideo"], StringComparer.OrdinalIgnoreCase);

        // What every JPEG file and every PNG file starts with.
        private static readonly byte[][] ImageSignatures =
        [
            [0xFF, 0xD8, 0xFF],
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        ];

        /// <exception cref="RequestRefusedException">
        /// When the file is empty, too large, or not a JPEG or PNG image.
        /// </exception>
        public static void ValidateImage(IFormFile file)
        {
            ValidateSize(file);

            if (!MediaFile.ImageExtensions.Contains(Path.GetExtension(file.FileName))
                || !ImageContentTypes.Contains(file.ContentType)
                || !StartsWithImageSignature(file))
            {
                throw new RequestRefusedException(UnsupportedImage);
            }
        }

        /// <exception cref="RequestRefusedException">
        /// When the file is empty, too large, or not an MP4, MOV or AVI video.
        /// Whether it really is one is found out when its frames are read.
        /// </exception>
        public static void ValidateVideo(IFormFile file)
        {
            ValidateSize(file);

            if (!MediaFile.VideoExtensions.Contains(Path.GetExtension(file.FileName))
                || !VideoContentTypes.Contains(file.ContentType))
            {
                throw new RequestRefusedException(UnsupportedVideo);
            }
        }

        private static void ValidateSize(IFormFile file)
        {
            if (file.Length == 0)
            {
                throw new RequestRefusedException(EmptyFile);
            }

            if (file.Length > MaxFileSize)
            {
                throw new RequestRefusedException(string.Format(FileTooLarge, MaxFileSize / BytesPerMegabyte));
            }
        }

        private static bool StartsWithImageSignature(IFormFile file)
        {
            var start = new byte[ImageSignatures.Max(signature => signature.Length)];

            using var content = file.OpenReadStream();
            var read = content.ReadAtLeast(start, start.Length, throwOnEndOfStream: false);

            return ImageSignatures.Any(signature =>
                read >= signature.Length && start.AsSpan(0, signature.Length).SequenceEqual(signature));
        }
    }
}
