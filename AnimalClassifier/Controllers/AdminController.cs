namespace AnimalClassifier.Controllers
{
    using AnimalClassifier.Core.Constants;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.ComponentModel.DataAnnotations;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService adminService;
        private readonly IUploadService uploadService;

        public AdminController(IAdminService adminService, IUploadService uploadService)
        {
            this.adminService = adminService;
            this.uploadService = uploadService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery, Range(1, int.MaxValue)] int page = 1) =>
            Ok(await adminService.GetUsersAsync(search, page));

        [HttpGet("users/{id}/history")]
        public async Task<IActionResult> GetUserHistory(string id) =>
            Ok(await uploadService.GetHistoryAsync(id));

        [HttpPost("users/{id}/lock")]
        public Task<IActionResult> LockUser(string id) =>
            ChangeUserAsync(adminService.LockUserAsync, id);

        [HttpPost("users/{id}/unlock")]
        public Task<IActionResult> UnlockUser(string id) =>
            ChangeUserAsync(adminService.UnlockUserAsync, id);

        [HttpPost("users/{id}/grant-admin")]
        public Task<IActionResult> GrantAdmin(string id) =>
            ChangeUserAsync(adminService.GrantAdminAsync, id);

        [HttpPost("users/{id}/revoke-admin")]
        public Task<IActionResult> RevokeAdmin(string id) =>
            ChangeUserAsync(adminService.RevokeAdminAsync, id);

        [HttpGet("audit")]
        public async Task<IActionResult> GetAuditLog([FromQuery, Range(1, int.MaxValue)] int page = 1) =>
            Ok(await adminService.GetAuditLogAsync(page));

        private async Task<IActionResult> ChangeUserAsync(Func<string, string, Task> change, string userId)
        {
            try
            {
                await change(User.Id()!, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
