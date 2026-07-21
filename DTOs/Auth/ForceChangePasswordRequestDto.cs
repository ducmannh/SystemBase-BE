using System.ComponentModel.DataAnnotations;

namespace SystemBase.BE.DTOs.Auth
{
    public class ForceChangePasswordRequestDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
