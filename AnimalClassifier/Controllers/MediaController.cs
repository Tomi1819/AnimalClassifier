namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.Media;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.StaticFiles;

    /// <summary>
    /// The uploaded images and videos, each loaded by a link a page was
    /// answered with. A browser loads one from an image or a video tag, which
    /// sends no token, so the link is what grants it and nobody signs in here.
    /// </summary>
    [Route(MediaLinkService.Route)]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private const string UnknownContentType = "application/octet-stream";

        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        private readonly IMediaLinkService mediaLinks;

        public MediaController(IMediaLinkService mediaLinks)
        {
            this.mediaLinks = mediaLinks;
        }

        /// <summary>
        /// Kept by the browser alone, and for no longer than a link lasts, so
        /// that no cache between them holds on to a user's photo. A video is
        /// answered in ranges, so that it can be played from any point.
        /// </summary>
        [HttpGet("{token}")]
        [ResponseCache(Duration = MediaLinkService.LifetimeMinutes * 60, Location = ResponseCacheLocation.Client)]
        public IActionResult GetMedia(string token)
        {
            var path = mediaLinks.GetPath(token);

            return PhysicalFile(path, ContentTypes.TryGetContentType(path, out var contentType) ? contentType : UnknownContentType,
                enableRangeProcessing: true);
        }
    }
}
