using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Services.ActionLog;
using System.Threading.Tasks;

namespace SystemBase.BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ActionLogController : ControllerBase
    {
        private readonly IActionLogService _actionLogService;

        public ActionLogController(IActionLogService actionLogService)
        {
            _actionLogService = actionLogService;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
        {
            var result = await _actionLogService.GetPagedAsync(request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(global::System.Guid id)
        {
            var result = await _actionLogService.DeleteAsync(id);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("multiple")]
        public async Task<IActionResult> DeleteMany([FromBody] global::System.Collections.Generic.List<global::System.Guid> ids)
        {
            var result = await _actionLogService.DeleteManyAsync(ids);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}
