using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement;

[Table("SystemSecuritySettings")]
public class SystemSecuritySetting
{
    [Key]
    public Guid Id { get; set; }

    public int MinPasswordLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true; // Chữ cái hoa
    public bool RequireLowercase { get; set; } = true; // Chữ cái thường
    public bool RequireNumber { get; set; } = true; // Số
    public bool RequireSpecialCharacter { get; set; } = true; // Ký tự đặc biệt
    public int PasswordExpiryDays { get; set; } = 90; // Thời gian mật khẩu hợp lệ (tính bằng ngày). 0 = không bao giờ hết hạn
    public int MaxFailedAccessAttempts { get; set; } = 5; // Giới hạn số lần đăng nhập sai
    public int LockoutDurationMinutes { get; set; } = 15; // Thời gian khóa tài khoản (phút)
    public string? AllowedAdminIPs { get; set; } // Giới hạn IP truy cập (cách nhau bởi dấu phẩy, rỗng = tất cả)
}
