using System;
using System.Threading.Tasks;
using Dapper;
using SystemBase.BE.DTOs.System.SystemSecuritySetting;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Infrastructure.Dapper;

namespace SystemBase.BE.Services.System.SystemSecuritySetting
{
    public class SystemSecuritySettingService : ISystemSecuritySettingService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SystemSecuritySettingService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Models.SystemManagement.SystemSecuritySetting> GetSettingInternalAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var setting = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.SystemSecuritySetting>("SELECT TOP 1 * FROM SystemSecuritySettings");
            
            if (setting == null)
            {
                setting = new Models.SystemManagement.SystemSecuritySetting
                {
                    Id = Guid.NewGuid(),
                    MinPasswordLength = 8,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireNumber = true,
                    RequireSpecialCharacter = true,
                    PasswordExpiryDays = 90,
                    MaxFailedAccessAttempts = 5,
                    LockoutDurationMinutes = 15,
                    AllowedAdminIPs = ""
                };
                
                await connection.ExecuteAsync(@"
                    INSERT INTO SystemSecuritySettings (Id, MinPasswordLength, RequireUppercase, RequireLowercase, RequireNumber, RequireSpecialCharacter, PasswordExpiryDays, MaxFailedAccessAttempts, LockoutDurationMinutes, AllowedAdminIPs) 
                    VALUES (@Id, @MinPasswordLength, @RequireUppercase, @RequireLowercase, @RequireNumber, @RequireSpecialCharacter, @PasswordExpiryDays, @MaxFailedAccessAttempts, @LockoutDurationMinutes, @AllowedAdminIPs)", 
                    setting);
            }
            
            return setting;
        }

        public async Task<RestData<SystemSecuritySettingDto>> GetSettingAsync()
        {
            var setting = await GetSettingInternalAsync();
            var dto = new SystemSecuritySettingDto
            {
                Id = setting.Id,
                MinPasswordLength = setting.MinPasswordLength,
                RequireUppercase = setting.RequireUppercase,
                RequireLowercase = setting.RequireLowercase,
                RequireNumber = setting.RequireNumber,
                RequireSpecialCharacter = setting.RequireSpecialCharacter,
                PasswordExpiryDays = setting.PasswordExpiryDays,
                MaxFailedAccessAttempts = setting.MaxFailedAccessAttempts,
                LockoutDurationMinutes = setting.LockoutDurationMinutes,
                AllowedAdminIPs = setting.AllowedAdminIPs
            };

            return new RestData<SystemSecuritySettingDto> { Data = dto, IsSuccess = true };
        }

        public async Task<RestData<SystemSecuritySettingDto>> UpdateSettingAsync(UpdateSystemSecuritySettingDto dto, Guid currentUserId)
        {
            var setting = await GetSettingInternalAsync();
            using var connection = _connectionFactory.CreateConnection();
            
            setting.MinPasswordLength = dto.MinPasswordLength;
            setting.RequireUppercase = dto.RequireUppercase;
            setting.RequireLowercase = dto.RequireLowercase;
            setting.RequireNumber = dto.RequireNumber;
            setting.RequireSpecialCharacter = dto.RequireSpecialCharacter;
            setting.PasswordExpiryDays = dto.PasswordExpiryDays;
            setting.MaxFailedAccessAttempts = dto.MaxFailedAccessAttempts;
            setting.LockoutDurationMinutes = dto.LockoutDurationMinutes;
            setting.AllowedAdminIPs = dto.AllowedAdminIPs;

            await connection.ExecuteAsync(@"
                UPDATE SystemSecuritySettings SET 
                    MinPasswordLength = @MinPasswordLength,
                    RequireUppercase = @RequireUppercase,
                    RequireLowercase = @RequireLowercase,
                    RequireNumber = @RequireNumber,
                    RequireSpecialCharacter = @RequireSpecialCharacter,
                    PasswordExpiryDays = @PasswordExpiryDays,
                    MaxFailedAccessAttempts = @MaxFailedAccessAttempts,
                    LockoutDurationMinutes = @LockoutDurationMinutes,
                    AllowedAdminIPs = @AllowedAdminIPs
                WHERE Id = @Id", setting);

            return await GetSettingAsync();
        }
    }
}
