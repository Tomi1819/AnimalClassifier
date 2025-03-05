namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService uploadService;

        public UploadController(IUploadService uploadService)
        {
            this.uploadService = uploadService;
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> UploadImageAsync([FromForm] IFormFile formFile)
        {
            if (formFile is null || formFile.Length == 0)
            {
                return BadRequest("The file is empty.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User is not authenticated.");
            }

            try
            {
                var result = await uploadService.UploadImageAsync(formFile, userId);
                return CreatedAtAction(nameof(GetRecognitionLogById), new { id = result.ImageId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AnimalRecognitionLog>> GetRecognitionLogById(int id)
        {
            var recognitionLog = await uploadService.GetRecognitionLogByIdAsync(id);

            if (recognitionLog is null)
            {
                return NotFound();
            }

            return recognitionLog;
        }
    }
}
