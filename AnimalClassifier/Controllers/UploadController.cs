namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly IRepository repository;

        public UploadController(IWebHostEnvironment hostingEnvironment, IRepository repository)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.repository = repository;
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImageAsync([FromForm] IFormFile formFile)
        {
            if (formFile is null || formFile.Length == 0)
            {
                return BadRequest("The file is empty");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(formFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid extension");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User is not authenticated.");
            }

            string uploadsFolder = Path.Combine(hostingEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, formFile.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            var animalRecognitionLog = new AnimalRecognitionLog()
            {
                ImagePath = $"/uploads/{formFile.FileName}",
                DateRecognized = DateTime.Now,
                UserId = userId
            };

            await repository.AddRecognitionLogAsync(animalRecognitionLog);
            await repository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecognitionLogById), new { id = animalRecognitionLog.Id }, animalRecognitionLog);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AnimalRecognitionLog>> GetRecognitionLogById(int id)
        {
            var recognitionLog = await repository.GetRecognitionLogByIdAsync(id);

            if (recognitionLog is null)
            {
                return NotFound();
            }

            return recognitionLog;
        }
    }
}
