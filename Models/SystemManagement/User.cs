using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement;

[Table("User")]
public class User
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid UserCreated { get; set; }

    public Guid UserModified { get; set; }

    [Required]
    [MaxLength(500)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string PasswordHashed { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Email { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpired { get; set; }

    [MaxLength(500)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    public string? AvatarPath { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime? LastPasswordChangedAt { get; set; }
    
    public int FailedAccessAttempts { get; set; } = 0;
    
    public DateTime? LockoutEnd { get; set; }

    public bool RequiresPasswordChange { get; set; } = false;

    public string? ResetPasswordCode { get; set; }

    public DateTime? ResetPasswordCodeExpiredAt { get; set; }
}