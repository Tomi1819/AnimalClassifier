namespace AnimalClassifier.Core.Recognitions.Media
{
    using AnimalClassifier.Core.Common.Exceptions;

    /// <summary>
    /// The links an uploaded image or video is loaded by, in place of a path
    /// anyone could open.
    ///
    /// A browser loads one with a plain request, from an image or a video tag,
    /// which carries no token, so the link itself is what grants it. It is
    /// encrypted and signed, so neither the user it belongs to nor the file can
    /// be read from it, and no other link can be made from it. It runs out, so
    /// one that is passed on stops working, and it stops working at once when
    /// its file is deleted.
    /// </summary>
    public interface IMediaLinkService
    {
        /// <summary>
        /// A link to one of a user's files, relative to the API.
        /// </summary>
        string CreateLink(string userId, string fileName);

        /// <summary>
        /// Where on disk the file a link names is.
        /// </summary>
        /// <param name="token">The link's last segment.</param>
        /// <exception cref="NotFoundException">
        /// When the link was not made here, was tampered with or has run out,
        /// or its file is gone. These are not told apart, since the caller can
        /// do nothing about any of them but ask for the page again.
        /// </exception>
        string GetPath(string token);
    }
}
