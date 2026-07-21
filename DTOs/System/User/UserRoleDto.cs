using System;

namespace SystemBase.BE.DTOs.User
{
    public class UserRoleDto
    {
        public Guid RoleId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsGranted { get; set; }
    }
}
