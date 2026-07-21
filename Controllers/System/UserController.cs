using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.User;
using SystemBase.BE.Services.User;
using SystemBase.BE.Services.ActionLog;

namespace SystemBase.BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly IActionLogService _actionLogService;

        public UserController(IUserService service, IActionLogService actionLogService)
        {
            _service = service;
            _actionLogService = actionLogService;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
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

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();
            
            var result = await _service.GetByIdAsync(userId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _service.CreateAsync(dto, userId);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Tạo mới",
                    Module = "Quản lý người dùng",
                    Description = $"Tạo mới người dùng: {dto.UserName}"
                });
            }

            if (!result.IsSuccess) return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _service.UpdateAsync(id, dto, userId);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Cập nhật",
                    Module = "Quản lý người dùng",
                    Description = $"Cập nhật người dùng: {dto.UserName}"
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
                    Module = "Quản lý người dùng",
                    Description = $"Xóa người dùng"
                });
            }

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("multiple")]
        public async Task<IActionResult> DeleteMany([FromBody] List<Guid> ids)
        {
            var userId = GetCurrentUserId();
            var result = await _service.DeleteManyAsync(ids, userId);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Xóa nhiều",
                    Module = "Quản lý người dùng",
                    Description = $"Xóa {ids.Count} người dùng"
                });
            }

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetUserRoles(Guid id)
        {
            var result = await _service.GetUserRolesAsync(id);
            return Ok(result);
        }

        [HttpPut("{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(Guid id, [FromBody] UpdateUserRolesDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _service.UpdateUserRolesAsync(id, dto.RoleIds, userId);
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
