namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity;
    using AnimalClassifier.Core.Recognitions.Training;
    using AnimalClassifier.RateLimiting;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;

    /// <summary>
    /// The feedback users offer for training, which an administrator accepts
    /// before the model is trained on it.
    /// </summary>
    [Route("api/admin/feedback")]
    [ApiController]
    [Authorize(Roles = RoleConstants.Admin)]
    public class FeedbackReviewController : ControllerBase
    {
        private readonly IFeedbackReviewService reviewService;
        private readonly IFeedbackSummaryService summaryService;
        private readonly ITrainingDataExporter trainingDataExporter;

        public FeedbackReviewController(IFeedbackReviewService reviewService,
                                        IFeedbackSummaryService summaryService,
                                        ITrainingDataExporter trainingDataExporter)
        {
            this.reviewService = reviewService;
            this.summaryService = summaryService;
            this.trainingDataExporter = trainingDataExporter;
        }

        /// <summary>
        /// One page of the feedback in one state of review, waiting for one
        /// unless another is asked for, in the order it was given.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFeedback(
            [FromQuery] FeedbackReviewStatus status = FeedbackReviewStatus.Pending,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            CancellationToken cancellationToken = default) =>
            Ok(await reviewService.GetFeedbackAsync(status, page, cancellationToken));

        /// <summary>
        /// What the feedback says of the model: how often users agree with it,
        /// its most common mistakes, and the animals it does not know that
        /// users named most.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
            Ok(await summaryService.GetSummaryAsync(cancellationToken));

        /// <summary>
        /// Answers with a ZIP archive of the accepted feedback's images, in a
        /// folder per animal, to train the model on. It reads every accepted
        /// image, as an account's own export reads every upload, so it is
        /// limited the same way.
        /// </summary>
        [HttpGet("export")]
        [EnableRateLimiting(RateLimitPolicies.DataExport)]
        public async Task<IActionResult> ExportTrainingData(CancellationToken cancellationToken) =>
            File(await trainingDataExporter.ExportAsync(cancellationToken), MediaTypeNames.Application.Zip, ExportFileName());

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            await reviewService.AcceptAsync(id);

            return NoContent();
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            await reviewService.RejectAsync(id);

            return NoContent();
        }

        // Dated, so that exports made on different days sit side by side.
        private static string ExportFileName() => $"animal-classifier-training-{DateTime.UtcNow:yyyy-MM-dd}.zip";
    }
}
