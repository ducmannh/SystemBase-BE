using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SystemBase.BE.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SystemBase.BE.DTOs.Auth;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Models.SystemManagement;
using Dapper;
using Dapper.FastCrud;
using SystemBase.BE.Infrastructure.Dapper;

using SystemBase.BE.Services.System.SystemSecuritySetting;

namespace SystemBase.BE.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;
        private readonly ISystemSecuritySettingService _securitySettingService;
        private readonly SystemBase.BE.Services.System.Email.IEmailService _emailService;

        public AuthService(IDbConnectionFactory connectionFactory, IConfiguration configuration, ISystemSecuritySettingService securitySettingService, SystemBase.BE.Services.System.Email.IEmailService emailService)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
            _securitySettingService = securitySettingService;
            _emailService = emailService;
        }

        public async Task<RestData<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var userExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM [User] WHERE UserName = @UserName AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                new { UserName = request.UserName });

            if (userExists)
            {
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Tên đăng nhập đã tồn tại." };
            }

            var emailExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM [User] WHERE Email = @Email AND IsDeleted = 0) THEN 1 ELSE 0 END as BIT)", 
                new { Email = request.Email });

            if (emailExists)
            {
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Email đã tồn tại." };
            }

            var securitySetting = await _securitySettingService.GetSettingInternalAsync();

            if (request.Password.Length < securitySetting.MinPasswordLength)
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = $"Mật khẩu phải từ {securitySetting.MinPasswordLength} kí tự trở lên." };
            if (securitySetting.RequireUppercase && !request.Password.Any(char.IsUpper))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái hoa." };
            if (securitySetting.RequireLowercase && !request.Password.Any(char.IsLower))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái thường." };
            if (securitySetting.RequireNumber && !request.Password.Any(char.IsDigit))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ số." };
            if (securitySetting.RequireSpecialCharacter && !request.Password.Any(c => !char.IsLetterOrDigit(c)))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 kí tự đặc biệt." };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var vietnamTime = TimeHelper.GetVietnamTime();
            var refreshToken = GenerateRefreshToken();

            var newUser = new Models.SystemManagement.User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                PasswordHashed = passwordHash,
                Email = request.Email,
                Name = request.Name,
                CreatedAt = vietnamTime,
                UpdatedAt = vietnamTime,
                RefreshToken = refreshToken,
                RefreshTokenExpired = vietnamTime.AddDays(_configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationInDays", 7)),
                IsActive = true,
                IsDeleted = false,
                LastPasswordChangedAt = vietnamTime,
                RequiresPasswordChange = false,
                FailedAccessAttempts = 0
            };

            await connection.InsertAsync(newUser);

            return new RestData<AuthResponseDto>
            {
                IsSuccess = true,
                Message = "Đăng ký thành công",
                Data = new AuthResponseDto
                {
                    Id = newUser.Id,
                    UserName = newUser.UserName,
                    Email = newUser.Email,
                    Name = newUser.Name,
                    AvatarPath = newUser.AvatarPath,
                    Token = GenerateJwtToken(newUser),
                    RefreshToken = refreshToken
                }
            };
        }

        public async Task<RestData<AuthResponseDto>> LoginAsync(LoginRequestDto request, string ipAddress, string userAgent)
        {
            var vietnamTime = TimeHelper.GetVietnamTime();
            var securitySetting = await _securitySettingService.GetSettingInternalAsync();

            if (!string.IsNullOrWhiteSpace(securitySetting.AllowedAdminIPs))
            {
                var allowedIps = securitySetting.AllowedAdminIPs.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                if (!allowedIps.Contains(ipAddress))
                {
                    await AddLoginLogAsync(null, request.UserName, ipAddress, userAgent, "Failed", "IP không được phép truy cập.");
                    return new RestData<AuthResponseDto> { IsSuccess = false, Message = "IP không được phép truy cập." };
                }
            }

            using var connection = _connectionFactory.CreateConnection();
            
            var user = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE UserName = @UserName AND IsDeleted = 0", 
                new { UserName = request.UserName });

            if (user == null)
            {
                await AddLoginLogAsync(null, request.UserName, ipAddress, userAgent, "Failed", "Tên đăng nhập không tồn tại.");
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Tên đăng nhập không tồn tại." };
            }

            if (!user.IsActive)
            {
                await AddLoginLogAsync(user.Id, request.UserName, ipAddress, userAgent, "Failed", "Tài khoản chưa được kích hoạt.");
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Tài khoản chưa được kích hoạt." };
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > vietnamTime)
            {
                await AddLoginLogAsync(user.Id, request.UserName, ipAddress, userAgent, "Failed", "Tài khoản đang bị khóa tạm thời.");
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = $"Tài khoản đang bị khóa đến {user.LockoutEnd.Value:dd/MM/yyyy HH:mm:ss}." };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHashed))
            {
                user.FailedAccessAttempts += 1;
                string msg = "Mật khẩu không chính xác.";
                
                if (user.FailedAccessAttempts >= securitySetting.MaxFailedAccessAttempts)
                {
                    user.LockoutEnd = vietnamTime.AddMinutes(securitySetting.LockoutDurationMinutes);
                    msg = $"Bạn đã nhập sai mật khẩu quá {securitySetting.MaxFailedAccessAttempts} lần. Tài khoản bị khóa {securitySetting.LockoutDurationMinutes} phút.";
                }

                await connection.UpdateAsync(user);
                await AddLoginLogAsync(user.Id, request.UserName, ipAddress, userAgent, "Failed", msg);
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = msg };
            }

            user.FailedAccessAttempts = 0;
            user.LockoutEnd = null;

            if (securitySetting.PasswordExpiryDays > 0)
            {
                var lastChanged = user.LastPasswordChangedAt ?? user.CreatedAt;
                if (vietnamTime > lastChanged.AddDays(securitySetting.PasswordExpiryDays))
                // if (vietnamTime > lastChanged.AddMinutes(securitySetting.PasswordExpiryDays))
                {
                    user.RequiresPasswordChange = true;
                }
            }

            if (user.RequiresPasswordChange)
            {
                await connection.UpdateAsync(user);
                if (user.LastPasswordChangedAt == null)
                {
                    await AddLoginLogAsync(user.Id, request.UserName, ipAddress, userAgent, "Failed", "Đăng nhập lần đầu, yêu cầu đổi mật khẩu.");
                    return new RestData<AuthResponseDto> { IsSuccess = false, Message = "FirstTimeLogin" };
                }
                else
                {
                    await AddLoginLogAsync(user.Id, request.UserName, ipAddress, userAgent, "Failed", "Mật khẩu đã hết hạn.");
                    return new RestData<AuthResponseDto> { IsSuccess = false, Message = "PasswordExpired" };
                }
            }

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpired = vietnamTime.AddDays(_configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationInDays"));
            
            await connection.UpdateAsync(user);
            await AddLoginLogAsync(user.Id, request.UserName, ipAddress, userAgent, "Success", "Đăng nhập thành công");

            return new RestData<AuthResponseDto>
            {
                IsSuccess = true,
                Message = "Đăng nhập thành công",
                Data = new AuthResponseDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Name = user.Name,
                    AvatarPath = user.AvatarPath,
                    Token = GenerateJwtToken(user),
                    RefreshToken = refreshToken
                }
            };
        }
        public async Task<RestData<AuthResponseDto>> ForceChangePasswordAsync(ForceChangePasswordRequestDto request, string ipAddress, string userAgent)
        {
            var vietnamTime = TimeHelper.GetVietnamTime();
            using var connection = _connectionFactory.CreateConnection();
            
            var user = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE UserName = @UserName AND IsDeleted = 0", 
                new { UserName = request.UserName });

            if (user == null || !user.IsActive)
            {
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Tài khoản không hợp lệ." };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHashed))
            {
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu cũ không chính xác." };
            }

            var securitySetting = await _securitySettingService.GetSettingInternalAsync();

            if (request.NewPassword.Length < securitySetting.MinPasswordLength)
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = $"Mật khẩu phải từ {securitySetting.MinPasswordLength} kí tự trở lên." };
            if (securitySetting.RequireUppercase && !request.NewPassword.Any(char.IsUpper))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái hoa." };
            if (securitySetting.RequireLowercase && !request.NewPassword.Any(char.IsLower))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái thường." };
            if (securitySetting.RequireNumber && !request.NewPassword.Any(char.IsDigit))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ số." };
            if (securitySetting.RequireSpecialCharacter && !request.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 kí tự đặc biệt." };

            if (request.OldPassword == request.NewPassword)
            {
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Mật khẩu mới không được trùng mật khẩu cũ." };
            }

            user.PasswordHashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.LastPasswordChangedAt = vietnamTime;
            user.RequiresPasswordChange = false;
            user.FailedAccessAttempts = 0;
            user.LockoutEnd = null;

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpired = vietnamTime.AddDays(_configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationInDays"));
            
            await connection.UpdateAsync(user);
            await AddLoginLogAsync(user.Id, request.UserName, ipAddress, userAgent, "Success", "Đổi mật khẩu và đăng nhập thành công");

            return new RestData<AuthResponseDto>
            {
                IsSuccess = true,
                Message = "Đổi mật khẩu và đăng nhập thành công",
                Data = new AuthResponseDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Name = user.Name,
                    AvatarPath = user.AvatarPath,
                    Token = GenerateJwtToken(user),
                    RefreshToken = refreshToken
                }
            };
        }

        public async Task<RestData<AuthResponseDto>> RefreshAsync(string refreshToken)
        {
            var vietnamTime = TimeHelper.GetVietnamTime();
            using var connection = _connectionFactory.CreateConnection();
            var user = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE RefreshToken = @RefreshToken", 
                new { RefreshToken = refreshToken });
            
            if (user == null || user.RefreshTokenExpired < vietnamTime || user.IsDeleted || !user.IsActive)
            {
                return new RestData<AuthResponseDto> { IsSuccess = false, Message = "Refresh Token không hợp lệ hoặc đã hết hạn." };
            }

            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpired = vietnamTime.AddDays(_configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationInDays"));
            
            await connection.UpdateAsync(user);

            return new RestData<AuthResponseDto>
            {
                IsSuccess = true,
                Message = "Làm mới Token thành công",
                Data = new AuthResponseDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Name = user.Name,
                    AvatarPath = user.AvatarPath,
                    Token = GenerateJwtToken(user),
                    RefreshToken = newRefreshToken
                }
            };
        }

        public async Task<RestData<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            var user = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE Email = @Email AND IsDeleted = 0 AND IsActive = 1", 
                new { Email = request.Email });

            if (user == null)
            {
                return new RestData<string> { IsSuccess = false, Message = "Email không tồn tại trong hệ thống hoặc tài khoản đã bị khóa." };
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new RestData<string> { IsSuccess = false, Message = "Email người dùng không hợp lệ." };
            }

            var random = new Random();
            var code = random.Next(100000, 999999).ToString();
            
            user.ResetPasswordCode = code;
            user.ResetPasswordCodeExpiredAt = TimeHelper.GetVietnamTime().AddMinutes(5);
            
            await connection.UpdateAsync(user);

            var body = $@"
                <h3>Yêu cầu đặt lại mật khẩu</h3>
                <p>Xin chào <strong>{user.UserName}</strong>,</p>
                <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản trên hệ thống Quản Lý Khuyến Công.</p>
                <p>Mã xác nhận của bạn là: <strong style='font-size: 24px; color: #4F46E5;'>{code}</strong></p>
                <p>Mã này sẽ hết hạn sau 5 phút.</p>
                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
            ";

            await _emailService.SendEmailAsync(user.Email, "Mã xác nhận đặt lại mật khẩu", body);

            return new RestData<string> { IsSuccess = true, Message = "Mã xác nhận đã được gửi đến email của bạn." };
        }

        public async Task<RestData<string>> VerifyResetCodeAsync(VerifyResetCodeRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            var user = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE Email = @Email AND ResetPasswordCode = @Code AND IsDeleted = 0 AND IsActive = 1", 
                new { Email = request.Email, Code = request.Code });

            if (user == null)
            {
                return new RestData<string> { IsSuccess = false, Message = "Mã xác nhận không chính xác." };
            }

            if (user.ResetPasswordCodeExpiredAt < TimeHelper.GetVietnamTime())
            {
                return new RestData<string> { IsSuccess = false, Message = "Mã xác nhận đã hết hạn. Vui lòng yêu cầu mã mới." };
            }

            return new RestData<string> { IsSuccess = true, Message = "Mã xác nhận hợp lệ." };
        }

        public async Task<RestData<string>> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            var user = await connection.QueryFirstOrDefaultAsync<Models.SystemManagement.User>(
                "SELECT * FROM [User] WHERE Email = @Email AND ResetPasswordCode = @Code AND IsDeleted = 0 AND IsActive = 1", 
                new { Email = request.Email, Code = request.Code });

            if (user == null)
            {
                return new RestData<string> { IsSuccess = false, Message = "Mã xác nhận không chính xác." };
            }

            if (user.ResetPasswordCodeExpiredAt < TimeHelper.GetVietnamTime())
            {
                return new RestData<string> { IsSuccess = false, Message = "Mã xác nhận đã hết hạn. Vui lòng yêu cầu mã mới." };
            }

            var securitySetting = await _securitySettingService.GetSettingInternalAsync();

            if (request.NewPassword.Length < securitySetting.MinPasswordLength)
                return new RestData<string> { IsSuccess = false, Message = $"Mật khẩu phải từ {securitySetting.MinPasswordLength} kí tự trở lên." };
            if (securitySetting.RequireUppercase && !request.NewPassword.Any(char.IsUpper))
                return new RestData<string> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái hoa." };
            if (securitySetting.RequireLowercase && !request.NewPassword.Any(char.IsLower))
                return new RestData<string> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ cái thường." };
            if (securitySetting.RequireNumber && !request.NewPassword.Any(char.IsDigit))
                return new RestData<string> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 chữ số." };
            if (securitySetting.RequireSpecialCharacter && !request.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
                return new RestData<string> { IsSuccess = false, Message = "Mật khẩu phải bao gồm ít nhất 1 kí tự đặc biệt." };

            user.PasswordHashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.LastPasswordChangedAt = TimeHelper.GetVietnamTime();
            user.RequiresPasswordChange = false;
            user.FailedAccessAttempts = 0;
            user.LockoutEnd = null;
            user.ResetPasswordCode = null;
            user.ResetPasswordCodeExpiredAt = null;

            await connection.UpdateAsync(user);

            return new RestData<string> { IsSuccess = true, Message = "Đặt lại mật khẩu thành công." };
        }

        private string GenerateJwtToken(Models.SystemManagement.User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? "");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpirationInMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task AddLoginLogAsync(Guid? userId, string userName, string ipAddress, string userAgent, string status, string message)
        {
            using var connection = _connectionFactory.CreateConnection();
            var log = new Models.SystemManagement.LoginLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Status = status,
                Message = message,
                CreatedAt = TimeHelper.GetVietnamTime()
            };
            await connection.InsertAsync(log);
        }
    }
}
