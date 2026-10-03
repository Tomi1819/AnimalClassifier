namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.Statistics;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.ComponentModel.DataAnnotations;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        // A year, which is as far back as the activity chart goes.
        private const int MaxActivityDays = 365;

        private readonly IStatisticsService statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            this.statisticsService = statisticsService;
        }

        [HttpGet("total")]
        public async Task<IActionResult> GetTotal(CancellationToken cancellationToken) =>
            Ok(await statisticsService.GetTotalRecognitionsAsync(cancellationToken));

        [HttpGet("users")]
        public async Task<IActionResult> GetUniqueUsers(CancellationToken cancellationToken) =>
            Ok(await statisticsService.GetUserCountAsync(cancellationToken));

        [HttpGet("top-animal")]
        public async Task<IActionResult> GetTopAnimals(CancellationToken cancellationToken) =>
            Ok(await statisticsService.GetMostCommonAnimalsAsync(cancellationToken));

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity(
            [FromQuery, Range(1, MaxActivityDays)] int days = 30,
            [FromQuery] string? timeZone = null,
            CancellationToken cancellationToken = default) =>
            Ok(await statisticsService.GetDailyRecognitionCountsAsync(days, timeZone, cancellationToken));
    }
}
