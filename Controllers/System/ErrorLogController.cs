using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Services.ErrorLog;
using SystemBase.BE.Services.ActionLog;

namespace SystemBase.BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ErrorLogController : ControllerBase
    {
        private readonly IErrorLogService _errorLogService;
        private readonly IActionLogService _actionLogService;

        public ErrorLogController(IErrorLogService errorLogService, IActionLogService actionLogService)
        {
            _errorLogService = errorLogService;
            _actionLogService = actionLogService;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
        {
            var result = await _errorLogService.GetPagedAsync(request);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _errorLogService.DeleteAsync(id);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Xóa",
                    Module = "Nhật ký lỗi hệ thống",
                    Description = $"Xóa nhật ký lỗi hệ thống"
                });
            }

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("multiple")]
        public async Task<IActionResult> DeleteMany([FromBody] global::System.Collections.Generic.List<Guid> ids)
        {
            var result = await _errorLogService.DeleteManyAsync(ids);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Xóa nhiều",
                    Module = "Nhật ký lỗi hệ thống",
                    Description = $"Xóa {ids.Count} nhật ký lỗi hệ thống"
                });
            }

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}
