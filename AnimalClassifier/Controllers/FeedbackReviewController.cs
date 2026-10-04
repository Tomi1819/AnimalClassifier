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

        public FeedbackReviewController(IFeedbackReviewService reviewService)
        {
            this.reviewService = reviewService;
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
