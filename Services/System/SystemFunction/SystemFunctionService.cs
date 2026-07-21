using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.SystemFunction;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Helpers;
using SystemBase.BE.Models.SystemManagement;
using Dapper;
using Dapper.FastCrud;
using SystemBase.BE.Infrastructure.Dapper;
using Microsoft.AspNetCore.SignalR;
using SystemBase.BE.Hubs;

namespace SystemBase.BE.Services.SystemFunction
{
    public class SystemFunctionService : ISystemFunctionService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IHubContext<AppHub> _hubContext;

        public SystemFunctionService(IDbConnectionFactory connectionFactory, IHubContext<AppHub> hubContext)
        {
            _connectionFactory = connectionFactory;
            _hubContext = hubContext;
        }

        public async Task<RestData<IEnumerable<SystemFunctionDto>>> GetAllAsync(bool includeHidden = false)
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM SystemFunction WHERE IsDeleted = 0";
            if (!includeHidden)
            {
                sql += " AND IsShow = 1";
            }
            var allItems = await connection.QueryAsync<Models.SystemManagement.SystemFunction>(sql);
            
            var treeList = BuildTree(allItems);
            return new RestData<IEnumerable<SystemFunctionDto>> { Data = treeList, IsSuccess = true };
        }

        public async Task<RestData<IEnumerable<SystemFunctionDto>>> GetMyFunctionsAsync(Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var allowedFunctionIds = await connection.QueryAsync<Guid>(@"
                SELECT DISTINCT srf.FunctionId 
                FROM SystemRoleFunctions srf
                INNER JOIN UserRoles ur ON ur.RoleId = srf.RoleId
                WHERE ur.UserId = @UserId", new { UserId = userId });

            var allFunctions = await connection.QueryAsync<Models.SystemManagement.SystemFunction>(
                "SELECT * FROM SystemFunction WHERE IsDeleted = 0 AND IsShow = 1");
                
            var allowedSet = new HashSet<Guid>(allowedFunctionIds);
            
            bool added;
            do
            {
                added = false;
                var currentAllowed = allowedSet.ToList();
                foreach (var id in currentAllowed)
                {
                    var func = allFunctions.FirstOrDefault(f => f.Id == id);
                    if (func != null && func.ParentId != Guid.Empty)
                    {
                        if (allowedSet.Add(func.ParentId))
                        {
                            added = true;
                        }
                    }
                }
            } while (added);

            var allowedFunctions = allFunctions.Where(f => allowedSet.Contains(f.Id)).ToList();
            var treeList = BuildTree(allowedFunctions);
            
            return new RestData<IEnumerable<SystemFunctionDto>> { Data = treeList, IsSuccess = true };
        }

        public async Task<RestData<PagedResult<SystemFunctionDto>>> GetPagedAsync(PaginationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            var allItems = await connection.QueryAsync<Models.SystemManagement.SystemFunction>(
                "SELECT * FROM SystemFunction WHERE IsDeleted = 0");
            
            if (!string.IsNullOrEmpty(request.Keyword))
            {
                var lowerKeyword = request.Keyword.ToLower();
                var matchedIds = allItems.Where(x => x.Code.ToLower().Contains(lowerKeyword) || 
                                               x.Name.ToLower().Contains(lowerKeyword) ||
                                               (x.Url != null && x.Url.ToLower().Contains(lowerKeyword)))
                                         .Select(x => x.Id).ToList();

                var allowedSet = new HashSet<Guid>(matchedIds);
                bool added;
                do
                {
                    added = false;
                    var currentAllowed = allowedSet.ToList();
                    foreach (var id in currentAllowed)
                    {
                        var func = allItems.FirstOrDefault(f => f.Id == id);
                        if (func != null && func.ParentId != Guid.Empty)
                        {
                            if (allowedSet.Add(func.ParentId))
                            {
                                added = true;
                            }
                        }
                    }
                } while (added);

                allItems = allItems.Where(f => allowedSet.Contains(f.Id)).ToList();
            }

            var topLevelItems = allItems.Where(x => x.ParentId == Guid.Empty).OrderBy(x => x.Order).ToList();
            var totalCount = topLevelItems.Count;
            
            if (totalCount > 0 && (request.PageIndex - 1) * request.PageSize >= totalCount)
            {
                request.PageIndex = 1;
            }

            var pagedTopLevel = topLevelItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var pagedTreeList = new List<SystemFunctionDto>();
            foreach (var item in pagedTopLevel)
            {
                pagedTreeList.Add(MapToDto(item));
                AddChildren(allItems, item.Id, pagedTreeList, 1);
            }

            var pagedResult = new PagedResult<SystemFunctionDto>(
                pagedTreeList, totalCount, request.PageIndex, request.PageSize);

            return new RestData<PagedResult<SystemFunctionDto>> { Data = pagedResult, IsSuccess = true };
        }

        public async Task<RestData<SystemFunctionDto?>> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var item = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.SystemFunction>(
                "SELECT * FROM SystemFunction WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
                
            if (item == null) return new RestData<SystemFunctionDto?> { IsSuccess = false, Message = "Không tìm thấy chức năng." };
            return new RestData<SystemFunctionDto?> { Data = MapToDto(item), IsSuccess = true };
        }

        public async Task<RestData<SystemFunctionDto>> CreateAsync(CreateSystemFunctionDto dto, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var codeExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM SystemFunction WHERE Code = @Code AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                new { Code = dto.Code });

            if (codeExists)
            {
                return new RestData<SystemFunctionDto> { IsSuccess = false, Message = "Tên mã chức năng đã tồn tại." };
            }

            var entity = new Models.SystemManagement.SystemFunction
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Name = dto.Name,
                Url = dto.Url,
                Order = dto.Order,
                Type = dto.Type,
                Icon = dto.Icon,
                IsShow = dto.IsShow,
                ParentId = dto.ParentId,
                CreatedAt = TimeHelper.GetVietnamTime(),
                UpdatedAt = TimeHelper.GetVietnamTime(),
                UserCreated = userId,
                UserModified = userId
            };

            await connection.InsertAsync(entity);
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemFunction", userId);

            return new RestData<SystemFunctionDto> { Data = MapToDto(entity), IsSuccess = true, Message = "Thêm mới thành công." };
        }

        public async Task<RestData<SystemFunctionDto?>> UpdateAsync(Guid id, UpdateSystemFunctionDto dto, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var entity = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.SystemFunction>(
                "SELECT * FROM SystemFunction WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
                
            if (entity == null) return new RestData<SystemFunctionDto?> { IsSuccess = false, Message = "Không tìm thấy chức năng." };

            var codeExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM SystemFunction WHERE Code = @Code AND Id != @Id AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                new { Code = dto.Code, Id = id });

            if (codeExists)
            {
                return new RestData<SystemFunctionDto?> { IsSuccess = false, Message = "Tên mã chức năng đã tồn tại." };
            }

            entity.Code = dto.Code;
            entity.Name = dto.Name;
            entity.Url = dto.Url;
            entity.Order = dto.Order;
            entity.Type = dto.Type;
            entity.Icon = dto.Icon;
            entity.IsShow = dto.IsShow;
            entity.ParentId = dto.ParentId;
            entity.UpdatedAt = TimeHelper.GetVietnamTime();
            entity.UserModified = userId;

            await connection.UpdateAsync(entity);
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemFunction", userId);
            return new RestData<SystemFunctionDto?> { Data = MapToDto(entity), IsSuccess = true, Message = "Cập nhật thành công." };
        }

        public async Task<RestData<bool>> DeleteAsync(Guid id, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var allFunctions = (await connection.QueryAsync<Models.SystemManagement.SystemFunction>("SELECT * FROM SystemFunction WHERE IsDeleted = 0")).ToList();
            
            var entity = allFunctions.FirstOrDefault(x => x.Id == id);
            if (entity == null) return new RestData<bool> { IsSuccess = false, Message = "Không tìm thấy chức năng." };

            var toDelete = new List<Models.SystemManagement.SystemFunction> { entity };
            GetDescendants(allFunctions, id, toDelete);

            foreach (var item in toDelete)
            {
                item.IsDeleted = true;
                item.UpdatedAt = TimeHelper.GetVietnamTime();
                item.UserModified = userId;
                await connection.UpdateAsync(item);
            }
            
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemFunction", userId);
            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Xóa thành công." };
        }

        public async Task<RestData<bool>> DeleteManyAsync(List<Guid> ids, Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var allFunctions = (await connection.QueryAsync<Models.SystemManagement.SystemFunction>("SELECT * FROM SystemFunction WHERE IsDeleted = 0")).ToList();
            
            var entities = allFunctions.Where(x => ids.Contains(x.Id)).ToList();
            if (!entities.Any()) return new RestData<bool> { IsSuccess = false, Message = "Không tìm thấy chức năng nào để xóa." };

            var toDelete = new List<Models.SystemManagement.SystemFunction>(entities);
            foreach (var entity in entities)
            {
                GetDescendants(allFunctions, entity.Id, toDelete);
            }

            toDelete = toDelete.GroupBy(x => x.Id).Select(g => g.First()).ToList();

            foreach (var item in toDelete)
            {
                item.IsDeleted = true;
                item.UpdatedAt = TimeHelper.GetVietnamTime();
                item.UserModified = userId;
                await connection.UpdateAsync(item);
            }

            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemFunction", userId);
            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Xóa nhiều thành công." };
        }

        private void GetDescendants(IEnumerable<Models.SystemManagement.SystemFunction> all, Guid parentId, List<Models.SystemManagement.SystemFunction> descendants)
        {
            var children = all.Where(x => x.ParentId == parentId).ToList();
            foreach (var child in children)
            {
                descendants.Add(child);
                GetDescendants(all, child.Id, descendants);
            }
        }

        private SystemFunctionDto MapToDto(Models.SystemManagement.SystemFunction entity)
        {
            return new SystemFunctionDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Url = entity.Url,
                Order = entity.Order,
                Type = entity.Type,
                Icon = entity.Icon,
                IsShow = entity.IsShow,
                ParentId = entity.ParentId
            };
        }

        private List<SystemFunctionDto> BuildTree(IEnumerable<Models.SystemManagement.SystemFunction> allFunctions)
        {
            var treeList = new List<SystemFunctionDto>();
            var topLevel = allFunctions.Where(x => x.ParentId == Guid.Empty).OrderBy(x => x.Order).ToList();
            
            foreach (var item in topLevel)
            {
                treeList.Add(MapToDto(item));
                AddChildren(allFunctions, item.Id, treeList, 1);
            }
            
            return treeList;
        }

        private void AddChildren(IEnumerable<Models.SystemManagement.SystemFunction> allFunctions, Guid parentId, List<SystemFunctionDto> treeList, int level)
        {
            var children = allFunctions.Where(x => x.ParentId == parentId).OrderBy(x => x.Order).ToList();
            var prefix = string.Concat(Enumerable.Repeat("— ", level));
            
            foreach (var child in children)
            {
                var dto = MapToDto(child);
                dto.Name = "| " + prefix + dto.Name;
                treeList.Add(dto);
                AddChildren(allFunctions, child.Id, treeList, level + 1);
            }
        }
    }
}
