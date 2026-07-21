using System;

namespace SystemBase.BE.DTOs.Auth
{
    public class AuthResponseDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? AvatarPath { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
