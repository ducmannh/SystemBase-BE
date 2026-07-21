using System.Threading.Tasks;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.LoginLog;

namespace SystemBase.BE.Services.LoginLog
{
    public interface ILoginLogService
    {
        Task<RestData<PagedResult<LoginLogDto>>> GetPagedAsync(PaginationRequest request);
        Task<RestData<object>> DeleteAsync(global::System.Guid id);
        Task<RestData<object>> DeleteManyAsync(global::System.Collections.Generic.List<global::System.Guid> ids);
    }
}
