using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.SystemRole;
using SystemBase.BE.DTOs.Shared;

namespace SystemBase.BE.Services.SystemRole
{
    public interface ISystemRoleService
    {
        Task<RestData<IEnumerable<SystemRoleDto>>> GetAllAsync();
        Task<RestData<PagedResult<SystemRoleDto>>> GetPagedAsync(PaginationRequest request);
        Task<RestData<SystemRoleDto?>> GetByIdAsync(Guid id);
        Task<RestData<SystemRoleDto>> CreateAsync(CreateSystemRoleDto dto, Guid userId);
        Task<RestData<SystemRoleDto?>> UpdateAsync(Guid id, UpdateSystemRoleDto dto, Guid userId);
        Task<RestData<bool>> DeleteAsync(Guid id, Guid userId);
        Task<RestData<bool>> DeleteManyAsync(List<Guid> ids, Guid userId);
        
        Task<RestData<IEnumerable<RoleFunctionDto>>> GetRoleFunctionsAsync(Guid roleId);
        Task<RestData<bool>> UpdateRoleFunctionsAsync(Guid roleId, List<Guid> functionIds, Guid userId);
    }
}
