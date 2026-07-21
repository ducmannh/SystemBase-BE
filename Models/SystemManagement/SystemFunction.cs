using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemBase.BE.Models.SystemManagement;

[Table("SystemFunction")]
public class SystemFunction
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid UserCreated { get; set; }

    public Guid UserModified { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsShow { get; set; } 
    public Guid ParentId { get; set; }
    public bool IsDeleted { get; set; }
}