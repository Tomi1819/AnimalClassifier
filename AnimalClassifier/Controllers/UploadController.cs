namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService uploadService;
        private readonly ILogger<UploadController> logger;

        public UploadController(IUploadService uploadService, ILogger<UploadController> logger)
        {
            this.uploadService = uploadService;
            this.logger = logger;
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

            var userId = User.Id();

            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Upload attempt without authentication.");
                return Unauthorized("User is not authenticated.");
            }

            try
            {
                var result = await uploadService.UploadImageAsync(formFile, userId);
                var resourceUrl = Url.Action(nameof(GetRecognitionLogById), new { id = result.ImageId });

                return Created(resourceUrl, result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning($"Upload failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (IOException ex)
            {
                logger.LogError($"File saving error: {ex.Message}");
                return StatusCode(500, "Error while saving the file.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error during file upload: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("video")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> UploadVideoAsync([FromForm] IFormFile videoFile)
        {
            if (videoFile is null || videoFile.Length == 0)
            {
                return BadRequest("The video file is empty.");
            }

            var userId = User.Id();
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Video upload attempt without authentication.");
                return Unauthorized("User is not authenticated.");
            }

            try
            {
                var result = await uploadService.UploadVideoAsync(videoFile, userId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning($"Video upload failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (IOException ex)
            {
                logger.LogError($"Video file saving error: {ex.Message}");
                return StatusCode(500, "Error while saving the video.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error during video upload: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ImageUploadResult>> GetRecognitionLogById(int id)
        {
            var recognitionLog = await uploadService.GetRecognitionLogByIdAsync(id);

            if (recognitionLog is null)
            {
                logger.LogWarning($"Recognition log with ID {id} not found.");
                return NotFound();
            }

            var response = new ImageUploadResult
            {
                ImageId = recognitionLog.Id,
                ImagePath = recognitionLog.ImagePath,
                RecognizedAnimal = recognitionLog.AnimalName,
                DateRecognized = recognitionLog.DateRecognized
            };

            return Ok(response);
        }
    }
}
