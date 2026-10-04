namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.History;
    using AnimalClassifier.Core.Recognitions.Uploads;
    using AnimalClassifier.Extensions;
    using AnimalClassifier.RateLimiting;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;

    /// <summary>
    /// A signed-in user's uploads, and the history of what was recognised in
    /// them.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService uploadService;
        private readonly IRecognitionHistoryService historyService;

        public UploadController(IUploadService uploadService, IRecognitionHistoryService historyService)
        {
            this.uploadService = uploadService;
            this.historyService = historyService;
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadValidator.MaxRequestSize)]
        [EnableRateLimiting(RateLimitPolicies.Upload)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile formFile, CancellationToken cancellationToken)
        {
            var result = await uploadService.UploadImageAsync(User.RequiredId(), formFile, cancellationToken);

            return CreatedAtAction(nameof(GetImageUpload), new { id = result.ImageId }, result);
        }

        [HttpPost("video")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadValidator.MaxRequestSize)]
        [EnableRateLimiting(RateLimitPolicies.Upload)]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile videoFile, CancellationToken cancellationToken) =>
            Ok(await uploadService.UploadVideoAsync(User.RequiredId(), videoFile, cancellationToken));

        /// <summary>
        /// The signed-in user's own recognitions, most recent first.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(CancellationToken cancellationToken) =>
            Ok(await historyService.GetHistoryAsync(User.RequiredId(), cancellationToken));

        /// <summary>
        /// Clears the signed-in user's history. The recognitions are kept, so
        /// the statistics still count them, but they leave the history and
        /// the search.
        /// </summary>
        [HttpDelete("history")]
        public async Task<IActionResult> ClearHistory()
        {
            await historyService.ClearHistoryAsync(User.RequiredId());

            return NoContent();
        }

        /// <summary>
        /// One of the signed-in user's image uploads, which an upload's
        /// answer points to.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetImageUpload(int id, CancellationToken cancellationToken) =>
            Ok(await uploadService.GetImageUploadAsync(User.RequiredId(), id, cancellationToken));
    }
}
