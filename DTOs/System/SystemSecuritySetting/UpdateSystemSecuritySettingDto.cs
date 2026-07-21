using System;
using System.ComponentModel.DataAnnotations;

namespace SystemBase.BE.DTOs.System.SystemSecuritySetting
{
    public class UpdateSystemSecuritySettingDto
    {
        [Required]
        [Range(4, 50)]
        public int MinPasswordLength { get; set; }
        
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireNumber { get; set; }
        public bool RequireSpecialCharacter { get; set; }
        
        [Required]
        [Range(0, 3650)]
        public int PasswordExpiryDays { get; set; }
        
        [Required]
        [Range(0, 100)]
        public int MaxFailedAccessAttempts { get; set; }
        
        [Required]
        [Range(0, 10000)]
        public int LockoutDurationMinutes { get; set; }
        
        public string? AllowedAdminIPs { get; set; }
    }
}
