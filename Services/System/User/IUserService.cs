using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.User;

namespace SystemBase.BE.Services.User
{
    public interface IUserService
    {
        Task<RestData<PagedResult<UserDto>>> GetPagedAsync(PaginationRequest request);
        Task<RestData<UserDto?>> GetByIdAsync(Guid id);
        Task<RestData<UserDto>> CreateAsync(CreateUserDto dto, Guid currentUserId);
        Task<RestData<UserDto?>> UpdateAsync(Guid id, UpdateUserDto dto, Guid currentUserId);
        Task<RestData<bool>> DeleteAsync(Guid id, Guid currentUserId);
        Task<RestData<bool>> DeleteManyAsync(List<Guid> ids, Guid currentUserId);
        
        Task<RestData<IEnumerable<UserRoleDto>>> GetUserRolesAsync(Guid userId);
        Task<RestData<bool>> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, Guid currentUserId);
    }
}
