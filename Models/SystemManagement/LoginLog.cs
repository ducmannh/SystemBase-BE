using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement
{
    [Table("LoginLogs")]
    public class LoginLog
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        [MaxLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
