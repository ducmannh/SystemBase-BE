using System;

namespace SystemBase.BE.DTOs.System.SystemSecuritySetting
{
    public class SystemSecuritySettingDto
    {
        public Guid Id { get; set; }
        public int MinPasswordLength { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireNumber { get; set; }
        public bool RequireSpecialCharacter { get; set; }
        public int PasswordExpiryDays { get; set; }
        public int MaxFailedAccessAttempts { get; set; }
        public int LockoutDurationMinutes { get; set; }
        public string? AllowedAdminIPs { get; set; }
    }
}
