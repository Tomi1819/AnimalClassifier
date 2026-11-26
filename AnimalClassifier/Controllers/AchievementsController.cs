namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.Achievements;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// The levels and badges the signed-in user has earned. Each takes the
    /// user's time zone, an IANA name such as <c>Europe/Sofia</c>, for the
    /// days counted in a row, and counts them in UTC without one.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AchievementsController : ControllerBase
    {
        private readonly IAchievementService achievementService;

        public AchievementsController(IAchievementService achievementService)
        {
            this.achievementService = achievementService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAchievements(
            [FromQuery] string? timeZone = null,
            CancellationToken cancellationToken = default) =>
            Ok(await achievementService.GetAchievementsAsync(User.RequiredId(), timeZone, cancellationToken));

        /// <summary>
        /// What one of the user's recognitions earned them, which the
        /// dashboard tells them once it is made, and again once they say
        /// whether the model was right.
        /// </summary>
        [HttpGet("recognitions/{recognitionId}")]
        public async Task<IActionResult> GetRecognitionAchievements(
            int recognitionId,
            [FromQuery] string? timeZone = null,
            CancellationToken cancellationToken = default) =>
            Ok(await achievementService.GetRecognitionAchievementsAsync(User.RequiredId(), recognitionId, timeZone, cancellationToken));
    }
}
