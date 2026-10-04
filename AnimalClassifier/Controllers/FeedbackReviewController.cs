namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Identity;
    using AnimalClassifier.Core.Recognitions.Training;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.ComponentModel.DataAnnotations;

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

        public FeedbackReviewController(IFeedbackReviewService reviewService, IFeedbackSummaryService summaryService)
        {
            this.reviewService = reviewService;
            this.summaryService = summaryService;
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
    }
}
