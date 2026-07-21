using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement
{
    [Table("ActionLogs")]
    public class ActionLog
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
