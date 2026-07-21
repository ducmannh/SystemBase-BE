using System.Threading.Tasks;
using SystemBase.BE.DTOs.System.SystemSecuritySetting;
using SystemBase.BE.DTOs.Shared;
using System;

namespace SystemBase.BE.Services.System.SystemSecuritySetting
{
    public interface ISystemSecuritySettingService
    {
        Task<RestData<SystemSecuritySettingDto>> GetSettingAsync();
        Task<RestData<SystemSecuritySettingDto>> UpdateSettingAsync(UpdateSystemSecuritySettingDto dto, Guid currentUserId);
        
        // Dành cho nội bộ để dùng cache hoặc query nhanh
        Task<Models.SystemManagement.SystemSecuritySetting> GetSettingInternalAsync();
    }
}
