using System;

using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement
{
    [Table("SystemRoleFunctions")]
    public class SystemRoleFunction
    {
        public Guid RoleId { get; set; }
        public Guid FunctionId { get; set; }
    }
}
