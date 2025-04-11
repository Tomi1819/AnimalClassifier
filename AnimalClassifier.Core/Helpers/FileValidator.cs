namespace AnimalClassifier.Core.Helpers
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.AspNetCore.Http;
    using System.Text.RegularExpressions;

    public class FileValidator : IFileValidator
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public void Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("The file is empty.");

            if (file.Length > MaxFileSize)
                throw new ArgumentException($"The file is too large. Max size: {MaxFileSize / (1024 * 1024)} MB.");

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("Unsupported file extension.");

            string mime = file.ContentType.ToLower();
            if (!Regex.IsMatch(mime, @"^image\/(jpeg|png)$"))
                throw new ArgumentException("Unsupported MIME type.");
        }
    }
}
