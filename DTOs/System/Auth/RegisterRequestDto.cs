using System.ComponentModel.DataAnnotations;

namespace SystemBase.BE.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(500)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Name { get; set; }
    }
}
