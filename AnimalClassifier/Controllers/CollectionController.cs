namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Recognitions.Collection;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// The animals the model knows, and which of them the signed-in user has
    /// found.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CollectionController : ControllerBase
    {
        private readonly IAnimalCollectionService collectionService;

        public CollectionController(IAnimalCollectionService collectionService)
        {
            this.collectionService = collectionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCollection(CancellationToken cancellationToken) =>
            Ok(await collectionService.GetCollectionAsync(User.RequiredId(), cancellationToken));
    }
}
