using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.ErrorLog;

namespace SystemBase.BE.Services.ErrorLog
{
    public interface IErrorLogService
    {
        Task LogAsync(Exception exception, string path);
        Task<RestData<PagedResult<ErrorLogDto>>> GetPagedAsync(PaginationRequest request);
        Task<RestData<object>> DeleteAsync(Guid id);
        Task<RestData<object>> DeleteManyAsync(List<Guid> ids);
    }
}
