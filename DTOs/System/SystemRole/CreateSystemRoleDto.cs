using System;
using System.ComponentModel.DataAnnotations;

namespace SystemBase.BE.DTOs.SystemRole
{
    public class CreateSystemRoleDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
    }
}
