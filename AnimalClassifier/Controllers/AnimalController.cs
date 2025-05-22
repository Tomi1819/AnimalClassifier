namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using static Constants.MessageConstants;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnimalController : ControllerBase
    {
        private readonly IAnimalService animalService;

        public AnimalController(IAnimalService animalService)
        {
            this.animalService = animalService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAnimals([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest(EnterSearchTerm);

            var results = await animalService.SearchAnimalByNameAsync(searchTerm);

            if (!results.Any())
                return NotFound(NoMatches);

            return Ok(results);
        }
    }
}
