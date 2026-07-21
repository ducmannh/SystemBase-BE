using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;

namespace SystemBase.BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new RestData<string> { IsSuccess = false, Message = "File không hợp lệ." });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new RestData<string> { IsSuccess = false, Message = "Dung lượng file ảnh tối đa là 10MB." });

            var ext = Path.GetExtension(file.FileName).ToLower();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExts.Contains(ext))
                return BadRequest(new RestData<string> { IsSuccess = false, Message = "Định dạng không hỗ trợ." });

            var folderPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/avatars/{fileName}";
            return Ok(new RestData<string> { Data = relativePath, IsSuccess = true });
        }
    }
}
