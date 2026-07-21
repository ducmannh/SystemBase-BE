using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.LoginLog;
using SystemBase.BE.Services.LoginLog;
using SystemBase.BE.Services.ActionLog;

namespace SystemBase.BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoginLogController : ControllerBase
    {
        private readonly ILoginLogService _loginLogService;
        private readonly IActionLogService _actionLogService;

        public LoginLogController(ILoginLogService loginLogService, IActionLogService actionLogService)
        {
            _loginLogService = loginLogService;
            _actionLogService = actionLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
        {
            var result = await _loginLogService.GetPagedAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(global::System.Guid id)
        {
            var result = await _loginLogService.DeleteAsync(id);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Xóa",
                    Module = "Nhật ký đăng nhập",
                    Description = $"Xóa nhật ký đăng nhập"
                });
            }

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("multiple")]
        public async Task<IActionResult> DeleteMany([FromBody] global::System.Collections.Generic.List<global::System.Guid> ids)
        {
            var result = await _loginLogService.DeleteManyAsync(ids);
            
            if (result.IsSuccess)
            {
                await _actionLogService.LogActionAsync(new DTOs.ActionLog.ActionLogRequest
                {
                    Action = "Xóa nhiều",
                    Module = "Nhật ký đăng nhập",
                    Description = $"Xóa {ids.Count} nhật ký đăng nhập"
                });
            }

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}
 