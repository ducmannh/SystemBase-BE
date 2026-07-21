using System;

namespace SystemBase.BE.DTOs.SystemRole
{
    public class SystemRoleDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
