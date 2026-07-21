using System;

namespace SystemBase.BE.DTOs.User
{
    public class UpdateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarPath { get; set; }
        public bool IsActive { get; set; }
    }
}
