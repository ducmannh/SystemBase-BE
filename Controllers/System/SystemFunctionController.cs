using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.SystemFunction;
using SystemBase.BE.Services.SystemFunction;
using SystemBase.BE.Services.ActionLog;

namespace SystemBase.BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SystemFunctionController : ControllerBase
    {
        private readonly ISystemFunctionService _service;
        private readonly IActionLogService _actionLogService;

        public SystemFunctionController(ISystemFunctionService service, IActionLogService actionLogService)
        {
            _service = service;
            _actionLogService = actionLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeHidden = false)
        {
            var result = await _service.GetAllAsync(includeHidden);
            return Ok(result);
        }

        [HttpGet("my-functions")]
        public async Task<IActionResult> GetMyFunctions()
        {
            var userId = GetCurrentUserId();
            var result = await _service.GetMyFunctionsAsync(userId);
            return Ok(result);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] SystemBase.BE.DTOs.Shared.PaginationRequest request)
        {
            var result = await _service.GetPagedAsync(request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSystemFunctionDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _service.CreateAsync(dto, userId);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Tạo mới",
                    Module = "Chức năng hệ thống",
                    Description = $"Tạo mới chức năng: {dto.Name}"
                });
            }

            if (!result.IsSuccess) return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSystemFunctionDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _service.UpdateAsync(id, dto, userId);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Cập nhật",
                    Module = "Chức năng hệ thống",
                    Description = $"Cập nhật chức năng: {dto.Name}"
                });
            }

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _service.DeleteAsync(id, userId);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Xóa",
                    Module = "Chức năng hệ thống",
                    Description = $"Xóa chức năng"
                });
            }

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("multiple")]
        public async Task<IActionResult> DeleteMany([FromBody] global::System.Collections.Generic.List<Guid> ids)
        {
            var userId = GetCurrentUserId();
            var result = await _service.DeleteManyAsync(ids, userId);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Xóa nhiều",
                    Module = "Chức năng hệ thống",
                    Description = $"Xóa {ids.Count} chức năng"
                });
            }

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue(global::System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            
            return Guid.Empty;
        }
    }
}
