using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.User;
using SystemBase.BE.Helpers;
using SystemBase.BE.Models.SystemManagement;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.SignalR;
using SystemBase.BE.Hubs;
using Dapper;
using Dapper.FastCrud;
using SystemBase.BE.Infrastructure.Dapper;

namespace SystemBase.BE.Services.User
{
    public class UserService : IUserService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<AppHub> _hubContext;
        private readonly SystemBase.BE.Services.System.SystemSecuritySetting.ISystemSecuritySettingService _securitySettingService;

        public UserService(IDbConnectionFactory connectionFactory, IWebHostEnvironment env, IHubContext<AppHub> hubContext, SystemBase.BE.Services.System.SystemSecuritySetting.ISystemSecuritySettingService securitySettingService)
        {
            _connectionFactory = connectionFactory;
            _env = env;
            _hubContext = hubContext;
            _securitySettingService = securitySettingService;
        }

        public async Task<RestData<PagedResult<UserDto>>> GetPagedAsync(PaginationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var whereClause = "IsDeleted = 0";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                whereClause += " AND (LOWER(UserName) LIKE @Keyword OR LOWER(Name) LIKE @Keyword OR LOWER(Email) LIKE @Keyword)";
                parameters.Add("Keyword", $"%{request.Keyword.ToLower()}%");
            }

            var countSql = $"SELECT COUNT(*) FROM [User] WHERE {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalCount > 0 && (request.PageIndex - 1) * request.PageSize >= totalCount)
            {
                request.PageIndex = 1;
            }

            var skip = (request.PageIndex - 1) * request.PageSize;
            parameters.Add("Skip", skip);
            parameters.Add("Take", request.PageSize);

            var sql = $"SELECT * FROM [User] WHERE {whereClause} ORDER BY CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var items = await connection.QueryAsync<Models.SystemManagement.User>(sql, parameters);

            var pagedResult = new PagedResult<UserDto>(
                items.Select(MapToDto).ToList(), totalCount, request.PageIndex, request.PageSize);

            return new RestData<PagedResult<UserDto>> { Data = pagedResult, IsSuccess = true };
        }

        public async Task<RestData<UserDto?>> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var item = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
            
            if (item == null) return new RestData<UserDto?> { IsSuccess = false, Message = "Không tìm thấy người dùng." };
            return new RestData<UserDto?> { Data = MapToDto(item), IsSuccess = true };
        }

        public async Task<RestData<UserDto>> CreateAsync(CreateUserDto dto, Guid currentUserId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var userNameExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM [User] WHERE UserName = @UserName AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                new { UserName = dto.UserName });
            if (userNameExists)
                return new RestData<UserDto> { IsSuccess = false, Message = "Tên đăng nhập đã tồn tại." };

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var emailExists = await connection.ExecuteScalarAsync<bool>(
                    "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM [User] WHERE Email = @Email AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                    new { Email = dto.Email });
                if (emailExists)
                    return new RestData<UserDto> { IsSuccess = false, Message = "Email đã tồn tại." };
            }

            var securitySetting = await _securitySettingService.GetSettingInternalAsync();

            if (dto.Password.Length < securitySetting.MinPasswordLength)
                return new RestData<UserDto> { IsSuccess = false, Message = $"Mật khẩu phải từ {securitySetting.MinPasswordLength} kí tự trở lên." };
            if (securitySetting.RequireUppercase && !dto.Password.Any(char.IsUpper))
                return new RestData<UserDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái hoa." };
            if (securitySetting.RequireLowercase && !dto.Password.Any(char.IsLower))
                return new RestData<UserDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái thường." };
            if (securitySetting.RequireNumber && !dto.Password.Any(char.IsDigit))
                return new RestData<UserDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ số." };
            if (securitySetting.RequireSpecialCharacter && !dto.Password.Any(c => !char.IsLetterOrDigit(c)))
                return new RestData<UserDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 kí tự đặc biệt." };

            var now = TimeHelper.GetVietnamTime();
            var entity = new Models.SystemManagement.User
            {
                Id = Guid.NewGuid(),
                UserName = dto.UserName,
                PasswordHashed = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = dto.Email,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                AvatarPath = dto.AvatarPath,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
                UserCreated = currentUserId,
                UserModified = currentUserId,
                IsDeleted = false,
                RequiresPasswordChange = true
            };

            await connection.InsertAsync(entity);
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemUser", currentUserId);

            return new RestData<UserDto> { Data = MapToDto(entity), IsSuccess = true, Message = "Tạo người dùng thành công." };
        }

        public async Task<RestData<UserDto?>> UpdateAsync(Guid id, UpdateUserDto dto, Guid currentUserId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var entity = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
                
            if (entity == null) return new RestData<UserDto?> { IsSuccess = false, Message = "Không tìm thấy người dùng." };

            if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != entity.UserName)
            {
                var userNameExists = await connection.ExecuteScalarAsync<bool>(
                    "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM [User] WHERE UserName = @UserName AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                    new { UserName = dto.UserName });
                if (userNameExists)
                    return new RestData<UserDto?> { IsSuccess = false, Message = "Tên đăng nhập đã tồn tại." };
                
                entity.UserName = dto.UserName;
            }

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != entity.Email)
            {
                var emailExists = await connection.ExecuteScalarAsync<bool>(
                    "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM [User] WHERE Email = @Email AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                    new { Email = dto.Email });
                if (emailExists)
                    return new RestData<UserDto?> { IsSuccess = false, Message = "Email đã tồn tại." };
            }

            if (!string.IsNullOrEmpty(dto.Password))
            {
                entity.PasswordHashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            if (entity.AvatarPath != dto.AvatarPath && !string.IsNullOrEmpty(entity.AvatarPath))
            {
                var oldFilePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), entity.AvatarPath.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            bool hasProfileChanged = false;
            
            if (entity.Email != dto.Email || 
                entity.Name != dto.Name || 
                entity.PhoneNumber != dto.PhoneNumber || 
                entity.AvatarPath != dto.AvatarPath || 
                entity.IsActive != dto.IsActive ||
                (!string.IsNullOrEmpty(dto.Password)) ||
                (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != entity.UserName))
            {
                hasProfileChanged = true;
            }

            entity.Email = dto.Email;
            entity.Name = dto.Name;
            entity.PhoneNumber = dto.PhoneNumber;
            entity.AvatarPath = dto.AvatarPath;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = TimeHelper.GetVietnamTime();
            entity.UserModified = currentUserId;

            await connection.UpdateAsync(entity);

            if (!dto.IsActive)
            {
                await _hubContext.Clients.User(id.ToString()).SendAsync("ForceLogout");
            }
            else if (id != currentUserId && hasProfileChanged)
            {
                await _hubContext.Clients.User(id.ToString()).SendAsync("ProfileUpdated");
            }

            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemUser", currentUserId);

            return new RestData<UserDto?> { Data = MapToDto(entity), IsSuccess = true, Message = "Cập nhật thành công." };
        }

        public async Task<RestData<bool>> DeleteAsync(Guid id, Guid currentUserId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var entity = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE Id = @Id AND IsDeleted = 0", new { Id = id });
                
            if (entity == null) return new RestData<bool> { IsSuccess = false, Message = "Không tìm thấy người dùng." };

            entity.IsDeleted = true;
            entity.UpdatedAt = TimeHelper.GetVietnamTime();
            entity.UserModified = currentUserId;

            await connection.UpdateAsync(entity);

            if (!string.IsNullOrEmpty(entity.AvatarPath))
            {
                var fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), entity.AvatarPath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }

            await _hubContext.Clients.User(id.ToString()).SendAsync("ForceLogout");
            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemUser", currentUserId);

            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Xóa thành công." };
        }

        public async Task<RestData<bool>> DeleteManyAsync(List<Guid> ids, Guid currentUserId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var idsString = string.Join("','", ids);
            var query = $"SELECT * FROM [User] WHERE Id IN ('{idsString}') AND IsDeleted = 0";
            var entities = await connection.QueryAsync<Models.SystemManagement.User>(query);
            
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = TimeHelper.GetVietnamTime();
                entity.UserModified = currentUserId;
                await connection.UpdateAsync(entity);

                if (!string.IsNullOrEmpty(entity.AvatarPath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), entity.AvatarPath.TrimStart('/'));
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }

                await _hubContext.Clients.User(entity.Id.ToString()).SendAsync("ForceLogout");
            }

            await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemUser", currentUserId);

            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Xóa nhiều thành công." };
        }

        public async Task<RestData<IEnumerable<UserRoleDto>>> GetUserRolesAsync(Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var allRoles = await connection.QueryAsync<Models.SystemManagement.SystemRole>(
                "SELECT * FROM SystemRole WHERE IsDeleted = 0 ORDER BY Name");
            
            var userRoleIds = await connection.QueryAsync<Guid>(
                "SELECT RoleId FROM UserRoles WHERE UserId = @UserId", new { UserId = userId });

            var list = allRoles.Select(role => new UserRoleDto
            {
                RoleId = role.Id,
                Code = role.Code,
                Name = role.Name,
                Description = role.Description,
                IsGranted = userRoleIds.Contains(role.Id)
            }).ToList();

            return new RestData<IEnumerable<UserRoleDto>> { Data = list, IsSuccess = true };
        }

        public async Task<RestData<bool>> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, Guid currentUserId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var user = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE Id = @UserId AND IsDeleted = 0", new { UserId = userId });
                
            if (user == null) return new RestData<bool> { IsSuccess = false, Message = "Không tìm thấy người dùng." };

            var existingRoleIds = await connection.QueryAsync<Guid>(
                "SELECT RoleId FROM UserRoles WHERE UserId = @UserId", new { UserId = userId });
            
            var existingRoleList = existingRoleIds.ToList();
            var newRoleList = roleIds ?? new List<Guid>();

            bool hasChanged = existingRoleList.Count != newRoleList.Count || 
                              existingRoleList.Except(newRoleList).Any() || 
                              newRoleList.Except(existingRoleList).Any();

            if (hasChanged)
            {
                await connection.ExecuteAsync("DELETE FROM UserRoles WHERE UserId = @UserId", new { UserId = userId });

                if (newRoleList.Any())
                {
                    foreach(var rId in newRoleList)
                    {
                        await connection.ExecuteAsync("INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
                            new { UserId = userId, RoleId = rId });
                    }
                }

                await _hubContext.Clients.User(userId.ToString()).SendAsync("PermissionsUpdated");
                await _hubContext.Clients.All.SendAsync("EntityChanged", "SystemUser", currentUserId);
            }
            return new RestData<bool> { Data = true, IsSuccess = true, Message = "Cập nhật nhóm quyền thành công." };
        }

        private UserDto MapToDto(Models.SystemManagement.User entity)
        {
            return new UserDto
            {
                Id = entity.Id,
                UserName = entity.UserName,
                Email = entity.Email,
                Name = entity.Name,
                PhoneNumber = entity.PhoneNumber,
                AvatarPath = entity.AvatarPath,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
