namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Uploads.Models;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Recognising the animal in an image or a video a user uploads. Each
    /// upload is stored, and recorded as a recognition in its owner's history.
    /// One that fails leaves neither behind.
    /// </summary>
    public interface IUploadService
    {
        /// <exception cref="RequestRefusedException">
        /// When the file is not a JPEG or PNG image, or is too large; see
        /// <see cref="UploadValidator"/>.
        /// </exception>
        Task<ImageUploadResult> UploadImageAsync(string userId, IFormFile file);

        /// <summary>
        /// Classifies the video a frame at a time, and records the animal seen
        /// most clearly in it, or <see cref="UploadService.UnrecognisedAnimal"/>
        /// when none was; see <see cref="VideoSummary"/>.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the file is not an MP4, MOV or AVI video, is too large, or
        /// cannot be read.
        /// </exception>
        Task<VideoUploadResult> UploadVideoAsync(string userId, IFormFile file);

        /// <summary>
        /// A recognition made from an image, as its upload answered it.
        /// </summary>
        /// <exception cref="NotFoundException">
        /// When it does not exist, or belongs to another user. The two are not
        /// told apart, so that ids cannot be tested for by asking.
        /// </exception>
        Task<ImageUploadResult> GetImageUploadAsync(string userId, int recognitionId, CancellationToken cancellationToken);
    }
}
