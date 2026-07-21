using System;

namespace SystemBase.BE.DTOs.User
{
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarPath { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
