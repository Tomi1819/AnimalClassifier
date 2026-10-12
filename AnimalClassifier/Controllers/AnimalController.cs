namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.Search;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Finding animals among the signed-in user's own images.
    /// </summary>
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
            Ok(await searchService.SearchAsync(User.RequiredId(), searchTerm, cancellationToken));
    }
}
