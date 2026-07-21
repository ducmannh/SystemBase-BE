using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.SystemRole;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Helpers;
using SystemBase.BE.Models.SystemManagement;
using Dapper;
using Dapper.FastCrud;
using SystemBase.BE.Infrastructure.Dapper;
using Microsoft.AspNetCore.SignalR;
using SystemBase.BE.Hubs;

namespace SystemBase.BE.Services.SystemRole
{
    public class SystemRoleService : ISystemRoleService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IHubContext<AppHub> _hubContext;

        public SystemRoleService(IDbConnectionFactory connectionFactory, IHubContext<AppHub> hubContext)
        {
            _connectionFactory = connectionFactory;
            _hubContext = hubContext;
        }

        public async Task<RestData<IEnumerable<SystemRoleDto>>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var items = await connection.QueryAsync<Models.SystemManagement.SystemRole>(
                "SELECT * FROM SystemRole WHERE IsDeleted = 0");
            return new RestData<IEnumerable<SystemRoleDto>> { Data = items.Select(MapToDto), IsSuccess = true };
        }

        public async Task<RestData<PagedResult<SystemRoleDto>>> GetPagedAsync(PaginationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            var whereClause = "IsDeleted = 0";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                whereClause += " AND (LOWER(Code) LIKE @Keyword OR LOWER(Name) LIKE @Keyword OR LOWER(Description) LIKE @Keyword)";
                parameters.Add("Keyword", $"%{request.Keyword.ToLower()}%");
            }

            var totalCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM SystemRole WHERE {whereClause}", parameters);

            if (totalCount > 0 && (request.PageIndex - 1) * request.PageSize >= totalCount)
            {
                request.PageIndex = 1;
            }

            var skip = (request.PageIndex - 1) * request.PageSize;
            parameters.Add("Skip", skip);
            parameters.Add("Take", request.PageSize);

            var sql = $"SELECT * FROM SystemRole WHERE {whereClause} ORDER BY CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var items = await connection.QueryAsync<Models.SystemManagement.SystemRole>(sql, parameters);

            var pagedResult = new PagedResult<SystemRoleDto>(
                items.Select(MapToDto), totalCount, request.PageIndex, request.PageSize);

            return new RestData<PagedResult<SystemRoleDto>> { Data = pagedResult, IsSuccess = true };
        }

        public async Task<RestData<SystemRoleDto?>> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var item = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.SystemRole>(
                "SELECT * FROM SystemRole WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
            if (item == null) return new RestData<SystemRoleDto?> { IsSuccess = false, Message = "Không tìm thấy quyền." };
            return new RestData<SystemRoleDto?> { Data = MapToDto(item), IsSuccess = true };
        }

        public async Task<RestData<SystemRoleDto>> CreateAsync(CreateSystemRoleDto dto, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var nameExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM SystemRole WHERE Name = @Name AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                new { Name = dto.Name });
            
            if (nameExists)
            {
                return new RestData<SystemRoleDto> { IsSuccess = false, Message = "Tên quyền đã tồn tại." };
            }

            var entity = new Models.SystemManagement.SystemRole
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = TimeHelper.GetVietnamTime(),
                UpdatedAt = TimeHelper.GetVietnamTime(),
                UserCreated = userId,
                UserModified = userId
            };

            await connection.InsertAsync(entity);
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemRole", userId);
            return new RestData<SystemRoleDto> { Data = MapToDto(entity), IsSuccess = true, Message = "Thêm mới thành công." };
        }

        public async Task<RestData<SystemRoleDto?>> UpdateAsync(Guid id, UpdateSystemRoleDto dto, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var entity = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.SystemRole>(
                "SELECT * FROM SystemRole WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
            
            if (entity == null) return new RestData<SystemRoleDto?> { IsSuccess = false, Message = "Không tìm thấy quyền." };

            var nameExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM SystemRole WHERE Name = @Name AND Id != @Id AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                new { Name = dto.Name, Id = id });

            if (nameExists)
            {
                return new RestData<SystemRoleDto?> { IsSuccess = false, Message = "Tên quyền đã tồn tại." };
            }

            entity.Code = dto.Code;
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.UpdatedAt = TimeHelper.GetVietnamTime();
            entity.UserModified = userId;

            await connection.UpdateAsync(entity);
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemRole", userId);
            return new RestData<SystemRoleDto?> { Data = MapToDto(entity), IsSuccess = true, Message = "Cập nhật thành công." };
        }

        public async Task<RestData<bool>> DeleteAsync(Guid id, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var entity = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.SystemRole>(
                "SELECT * FROM SystemRole WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
                
            if (entity == null) return new RestData<bool> { IsSuccess = false, Message = "Không tìm thấy quyền." };

            entity.IsDeleted = true;
            entity.UpdatedAt = TimeHelper.GetVietnamTime();
            entity.UserModified = userId;

            await connection.UpdateAsync(entity);
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemRole", userId);
            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Xóa thành công." };
        }

        public async Task<RestData<bool>> DeleteManyAsync(List<Guid> ids, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var idsString = string.Join("','", ids);
            var query = $"SELECT * FROM SystemRole WHERE Id IN ('{idsString}') AND IsDeleted = 0";
            var entities = await connection.QueryAsync<Models.SystemManagement.SystemRole>(query);
            
            if (!entities.Any()) return new RestData<bool> { IsSuccess = false, Message = "Không tìm thấy quyền nào để xóa." };

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = TimeHelper.GetVietnamTime();
                entity.UserModified = userId;
                await connection.UpdateAsync(entity);
            }

            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemRole", userId);
            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Xóa nhiều thành công." };
        }

        public async Task<RestData<IEnumerable<RoleFunctionDto>>> GetRoleFunctionsAsync(Guid roleId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var allFunctions = await connection.QueryAsync<Models.SystemManagement.SystemFunction>(
                "SELECT * FROM SystemFunction WHERE IsDeleted = 0 AND IsShow = 1");
            
            var roleFunctionIds = await connection.QueryAsync<Guid>(
                "SELECT FunctionId FROM SystemRoleFunctions WHERE RoleId = @RoleId", new { RoleId = roleId });

            var treeList = new List<RoleFunctionDto>();
            var topLevel = allFunctions.Where(x => x.ParentId == Guid.Empty).OrderBy(x => x.Order).ToList();

            foreach (var item in topLevel)
            {
                var dto = MapToRoleFunctionDto(item, roleFunctionIds.ToList());
                treeList.Add(dto);
                AddChildren(allFunctions, item.Id, treeList, 1, roleFunctionIds.ToList());
            }

            return new RestData<IEnumerable<RoleFunctionDto>> { Data = treeList, IsSuccess = true };
        }

        public async Task<RestData<bool>> UpdateRoleFunctionsAsync(Guid roleId, List<Guid> functionIds, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var role = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.SystemRole>(
                "SELECT * FROM SystemRole WHERE Id = @Id AND IsDeleted = 0", new { Id = roleId });
                
            if (role == null) return new RestData<bool> { IsSuccess = false, Message = "Không tìm thấy quyền." };

            var existingFuncIds = await connection.QueryAsync<Guid>(
                "SELECT FunctionId FROM SystemRoleFunctions WHERE RoleId = @RoleId", new { RoleId = roleId });
            
            var existingFuncList = existingFuncIds.ToList();
            var newFuncList = functionIds ?? new List<Guid>();

            bool hasChanged = existingFuncList.Count != newFuncList.Count || 
                              existingFuncList.Except(newFuncList).Any() || 
                              newFuncList.Except(existingFuncList).Any();

            if (hasChanged)
            {
                await connection.ExecuteAsync("DELETE FROM SystemRoleFunctions WHERE RoleId = @RoleId", new { RoleId = roleId });

                if (newFuncList.Any())
                {
                    foreach(var funcId in newFuncList)
                    {
                        await connection.ExecuteAsync("INSERT INTO SystemRoleFunctions (RoleId, FunctionId) VALUES (@RoleId, @FunctionId)",
                            new { RoleId = roleId, FunctionId = funcId });
                    }
                }
                
                role.UpdatedAt = TimeHelper.GetVietnamTime();
                role.UserModified = userId;
                await connection.UpdateAsync(role);

                var userIdsWithRole = await connection.QueryAsync<Guid>(
                    "SELECT UserId FROM UserRoles WHERE RoleId = @RoleId", new { RoleId = roleId });

                foreach (var uid in userIdsWithRole)
                {
                    await _hubContext.Clients.User(uid.ToString()).SendAsync("RolePermissionsUpdated", userId);
                }
            }

            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Cập nhật phân quyền thành công." };
        }

        private SystemRoleDto MapToDto(Models.SystemManagement.SystemRole entity)
        {
            return new SystemRoleDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description
            };
        }

        private RoleFunctionDto MapToRoleFunctionDto(Models.SystemManagement.SystemFunction entity, List<Guid> roleFunctionIds)
        {
            return new RoleFunctionDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Url = entity.Url,
                Order = entity.Order,
                Type = entity.Type,
                Icon = entity.Icon,
                IsShow = entity.IsShow,
                ParentId = entity.ParentId,
                IsGranted = roleFunctionIds.Contains(entity.Id)
            };
        }

        private void AddChildren(IEnumerable<Models.SystemManagement.SystemFunction> allFunctions, Guid parentId, List<RoleFunctionDto> treeList, int level, List<Guid> roleFunctionIds)
        {
            var children = allFunctions.Where(x => x.ParentId == parentId).OrderBy(x => x.Order).ToList();
            var prefix = string.Concat(Enumerable.Repeat("— ", level));
            
            foreach (var child in children)
            {
                var dto = MapToRoleFunctionDto(child, roleFunctionIds);
                dto.Name = "| " + prefix + dto.Name;
                treeList.Add(dto);
                AddChildren(allFunctions, child.Id, treeList, level + 1, roleFunctionIds);
            }
        }
    }
}
