using System.Threading.Tasks;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.ActionLog;

namespace SystemBase.BE.Services.ActionLog
{
    public interface IActionLogService
    {
        Task LogActionAsync(ActionLogRequest request);
        Task<RestData<PagedResult<ActionLogDto>>> GetPagedAsync(PaginationRequest request);
        Task<RestData<object>> DeleteAsync(global::System.Guid id);
        Task<RestData<object>> DeleteManyAsync(global::System.Collections.Generic.List<global::System.Guid> ids);
    }
}
