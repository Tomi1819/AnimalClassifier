namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.Search;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnimalController : ControllerBase
    {
        private readonly IAnimalSearchService searchService;

        public AnimalController(IAnimalSearchService searchService)
        {
            this.searchService = searchService;
        }

        /// <summary>
        /// Answers 404 when nothing matches, which the search page shows as
        /// an empty result rather than as an error.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchAnimals([FromQuery] string? searchTerm, CancellationToken cancellationToken) =>
            Ok(await searchService.SearchAsync(searchTerm, cancellationToken));
    }
}
