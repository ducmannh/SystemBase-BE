using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.SystemFunction;
using SystemBase.BE.DTOs.Shared;

namespace SystemBase.BE.Services.SystemFunction
{
    public interface ISystemFunctionService
    {
        Task<RestData<IEnumerable<SystemFunctionDto>>> GetAllAsync(bool includeHidden = false);
        Task<RestData<IEnumerable<SystemFunctionDto>>> GetMyFunctionsAsync(Guid userId);
        Task<RestData<PagedResult<SystemFunctionDto>>> GetPagedAsync(PaginationRequest request);
        Task<RestData<SystemFunctionDto?>> GetByIdAsync(Guid id);
        Task<RestData<SystemFunctionDto>> CreateAsync(CreateSystemFunctionDto dto, Guid userId);
        Task<RestData<SystemFunctionDto?>> UpdateAsync(Guid id, UpdateSystemFunctionDto dto, Guid userId);
        Task<RestData<bool>> DeleteAsync(Guid id, Guid userId);
        Task<RestData<bool>> DeleteManyAsync(List<Guid> ids, Guid userId);
    }
}
