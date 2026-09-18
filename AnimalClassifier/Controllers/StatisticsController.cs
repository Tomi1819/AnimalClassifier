namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using static Constants.MessageConstants;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            this.statisticsService = statisticsService;
        }

        [HttpGet("total")]
        public async Task<IActionResult> GetTotal() =>
            Ok(await statisticsService.GetTotalClassificationAsync());

        [HttpGet("users")]
        public async Task<IActionResult> GetUniqueUsers() =>
            Ok(await statisticsService.GetUniqueUserCountAsync());

        [HttpGet("top-animal")]
        public async Task<IActionResult> GetTopAnimals() =>
            Ok(await statisticsService.GetMostCommonAnimalAsync());

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity([FromQuery, Range(1, 365)] int days = 30, [FromQuery] string? timeZone = null)
        {
            TimeZoneInfo? zone = TimeZoneInfo.Utc;

            if (timeZone is not null && !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out zone))
            {
                return BadRequest(new { message = UnknownTimeZone });
            }

            return Ok(await statisticsService.GetDailyRecognitionCountsAsync(days, zone));
        }
    }
}
