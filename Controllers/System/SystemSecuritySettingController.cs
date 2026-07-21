using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.System.SystemSecuritySetting;
using SystemBase.BE.Services.System.SystemSecuritySetting;
using SystemBase.BE.Services.ActionLog;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace SystemBase.BE.Controllers.System
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SystemSecuritySettingController : ControllerBase
    {
        private readonly ISystemSecuritySettingService _service;
        private readonly IActionLogService _actionLogService;

        public SystemSecuritySettingController(ISystemSecuritySettingService service, IActionLogService actionLogService)
        {
            _service = service;
            _actionLogService = actionLogService;
        }

        private Guid GetCurrentUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _service.GetSettingAsync();
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSystemSecuritySettingDto dto)
        {
            var result = await _service.UpdateSettingAsync(dto, GetCurrentUserId());
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Cập nhật",
                    Module = "Cấu hình bảo mật hệ thống",
                    Description = "Cập nhật chính sách bảo mật cấp độ 2"
                });
            }
            return Ok(result);
        }
    }
}
