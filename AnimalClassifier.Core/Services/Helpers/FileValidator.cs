namespace AnimalClassifier.Core.Services.Helpers
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.AspNetCore.Http;
    using System.Text.RegularExpressions;

    public class FileValidator : IFileValidator
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov", ".avi" };
        private const long MaxFileSize = 5 * 1024 * 1024;
        public void ValidateImage(IFormFile file)
        {
            CommonValidate(file);

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedImageExtensions.Contains(extension))
                throw new ArgumentException("Unsupported file extension.");

            string mime = file.ContentType.ToLower();
            if (!Regex.IsMatch(mime, @"^image\/(jpeg|png)$"))
                throw new ArgumentException("Unsupported MIME type.");
        }
        public void ValidateVideo(IFormFile file)
        {
            CommonValidate(file);

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedVideoExtensions.Contains(extension))
                throw new ArgumentException("Unsupported video file extension.");

            string mime = file.ContentType.ToLower();
            if (!Regex.IsMatch(mime, @"^video\/(mp4|quicktime|x-msvideo)$"))
                throw new ArgumentException("Unsupported video MIME type.");

        }

        public bool IsImage(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return AllowedImageExtensions.Contains(extension);
        }
        private static void CommonValidate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("The file is empty.");

            if (file.Length > MaxFileSize)
                throw new ArgumentException($"The file is too large. Max size: {MaxFileSize / (1024 * 1024)} MB.");
        }
    }
}
