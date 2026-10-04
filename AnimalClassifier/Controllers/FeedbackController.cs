namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.Feedback;
    using AnimalClassifier.Core.Recognitions.Feedback.Models;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// What a signed-in user says of the animals recognised in their images.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            this.feedbackService = feedbackService;
        }

        /// <summary>
        /// The animals a correction can name, alphabetically.
        /// </summary>
        [HttpGet("animals")]
        public IActionResult GetKnownAnimals() =>
            Ok(feedbackService.GetKnownAnimals());

        /// <summary>
        /// Gives feedback on one of the user's image recognitions, in place of
        /// any they gave before, and answers with it as it is kept.
        /// </summary>
        [HttpPut("{recognitionId}")]
        public async Task<IActionResult> GiveFeedback(int recognitionId, [FromBody] FeedbackRequest request, CancellationToken cancellationToken) =>
            Ok(await feedbackService.GiveFeedbackAsync(User.RequiredId(), recognitionId, request, cancellationToken));

        [HttpDelete("{recognitionId}")]
        public async Task<IActionResult> WithdrawFeedback(int recognitionId)
        {
            await feedbackService.WithdrawFeedbackAsync(User.RequiredId(), recognitionId);

            return NoContent();
        }
    }
}
