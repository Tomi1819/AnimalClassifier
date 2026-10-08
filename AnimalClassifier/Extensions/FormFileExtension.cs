namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Recognitions.Uploads.Models;

    public static class FormFileExtension
    {
        /// <summary>
        /// The file as Core takes an upload, which knows nothing of the form
        /// it was posted in.
        /// </summary>
        public static UploadedFile ToUploadedFile(this IFormFile file) => new()
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            OpenReadStream = file.OpenReadStream
        };
    }
}
