namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Common.Exceptions;

    /// <summary>
    /// Decodes an uploaded image and encodes it afresh, which is then what is
    /// classified, stored and shown. Only the pixels make it across. What a
    /// camera writes beside them, such as where the photo was taken, is left
    /// behind, and the rotation it records is applied to the pixels instead.
    /// </summary>
    public interface IImageSanitizer
    {
        /// <param name="image">A JPEG or PNG file's bytes.</param>
        /// <param name="extension">The format to encode it in, such as <c>.jpg</c>.</param>
        /// <exception cref="RequestRefusedException">
        /// When the image has too many pixels, or cannot be decoded.
        /// </exception>
        byte[] Sanitize(byte[] image, string extension);
    }
}
