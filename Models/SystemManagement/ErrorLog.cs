using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement
{
    [Table("ErrorLogs")]
    public class ErrorLog
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string StackTrace { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
