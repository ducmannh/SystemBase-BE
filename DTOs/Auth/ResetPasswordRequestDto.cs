using System.ComponentModel.DataAnnotations;

namespace SystemBase.BE.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã xác nhận là bắt buộc")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
