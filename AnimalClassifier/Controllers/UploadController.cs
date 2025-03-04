namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly IRepository repository;

        private const long MaxFileSize = 5 * 1024 * 1024;

        public UploadController(IWebHostEnvironment hostingEnvironment, IRepository repository)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.repository = repository;
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

            if (formFile.Length > MaxFileSize)
            {
                return BadRequest($"The file size is too large. Maximum allowed size is {MaxFileSize / 1024 / 1024} MB.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(formFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file extension. Only .jpg, .jpeg, and .png are allowed.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User is not authenticated.");
            }

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";

            string uploadsFolder = Path.Combine(hostingEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            var animalRecognitionLog = new AnimalRecognitionLog()
            {
                ImagePath = $"/uploads/{uniqueFileName}",
                DateRecognized = DateTime.Now,
                UserId = userId
            };

            await repository.AddRecognitionLogAsync(animalRecognitionLog);
            await repository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecognitionLogById), new { id = animalRecognitionLog.Id }, new
            {
                ImageId = animalRecognitionLog.Id,
                ImagePath = animalRecognitionLog.ImagePath,
                DateRecognized = animalRecognitionLog.DateRecognized
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AnimalRecognitionLog>> GetRecognitionLogById(int id)
        {
            var recognitionLog = await repository.GetRecognitionLogByIdAsync(id);

            if (recognitionLog == null)
            {
                return NotFound();
            }

            return recognitionLog;
        }
    }
}
