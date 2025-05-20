namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

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
    }
}
