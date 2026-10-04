namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Common.Exceptions;
    using Microsoft.AspNetCore.Http;
    using static AnimalClassifier.Core.Recognitions.Uploads.UploadMessages;

    /// <summary>
    /// What an uploaded file has to be for a recognition to be made from it.
    ///
    /// Its name and content type are whatever the caller says they are, so its
    /// first bytes are checked as well. An image's are what the model reads,
    /// and anything that is not really an image would fail inside it. A
    /// video's are read by a decoder that knows many more formats than these,
    /// some of which can point it at other files, so it is given only these.
    /// </summary>
    public static class UploadValidator
    {
        /// <summary>
        /// The largest file accepted, image or video, in bytes.
        /// </summary>
        public const long MaxFileSize = 5 * BytesPerMegabyte;

        /// <summary>
        /// The largest request an upload is read from, in bytes. It leaves room
        /// for the form around the file, so that a file a little too large is
        /// still told how large it can be. A larger request is cut off unread.
        /// </summary>
        public const long MaxRequestSize = MaxFileSize + BytesPerMegabyte;

        private const long BytesPerMegabyte = 1024 * 1024;

        private const int MovieTypeBoxOffset = 4;
        private const int AviTypeOffset = 8;
        private const int VideoSignatureLength = 12;

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

        // An MP4 or MOV file starts with the box naming its type, after the
        // box's length, and an AVI file is a RIFF file whose type is AVI.
        private static readonly byte[] MovieTypeBox = "ftyp"u8.ToArray();
        private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
        private static readonly byte[] AviType = "AVI "u8.ToArray();

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
        /// Whether it can be played is found out when its frames are read.
        /// </exception>
        public static void ValidateVideo(IFormFile file)
        {
            ValidateSize(file);

            if (!MediaFile.VideoExtensions.Contains(Path.GetExtension(file.FileName))
                || !VideoContentTypes.Contains(file.ContentType)
                || !StartsWithVideoSignature(file))
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
            var start = ReadStart(file, ImageSignatures.Max(signature => signature.Length));

            return ImageSignatures.Any(signature => start.StartsWith(signature));
        }

        private static bool StartsWithVideoSignature(IFormFile file)
        {
            var start = ReadStart(file, VideoSignatureLength);

            return start.Length == VideoSignatureLength
                && (start.AsSpan(MovieTypeBoxOffset).StartsWith(MovieTypeBox)
                    || (start.AsSpan().StartsWith(RiffSignature) && start.AsSpan(AviTypeOffset).StartsWith(AviType)));
        }

        // As many of the bytes asked for as the file has.
        private static byte[] ReadStart(IFormFile file, int length)
        {
            var start = new byte[length];

            using var content = file.OpenReadStream();
            var read = content.ReadAtLeast(start, length, throwOnEndOfStream: false);

            return start[..read];
        }
    }
}
