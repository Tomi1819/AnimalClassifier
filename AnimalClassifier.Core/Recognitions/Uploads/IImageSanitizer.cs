namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Common.Exceptions;

    /// <summary>
    /// Decodes an uploaded image and encodes it afresh as a JPEG, which is
    /// then what is classified, stored and shown. Only the pixels make it
    /// across. What a camera writes beside them, such as where the photo was
    /// taken, is left behind, and the rotation it records is applied to the
    /// pixels instead.
    /// </summary>
    public interface IImageSanitizer
    {
        /// <param name="image">The file's bytes, in any format <see cref="UploadValidator"/> accepts.</param>
        /// <returns>The image as a JPEG, to be stored as one; see <see cref="MediaFile.StoredImageExtension"/>.</returns>
        /// <exception cref="RequestRefusedException">
        /// When the image has too many pixels, or cannot be decoded.
        /// </exception>
        byte[] Sanitize(byte[] image);
    }
}
