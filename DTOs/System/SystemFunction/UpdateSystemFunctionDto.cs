using System;
using System.ComponentModel.DataAnnotations;

namespace SystemBase.BE.DTOs.SystemFunction
{
    public class UpdateSystemFunctionDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Url { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsShow { get; set; } 
        public Guid ParentId { get; set; }
    }
}
