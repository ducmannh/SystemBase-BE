using System;

using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement
{
    [Table("UserRoles")]
    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
