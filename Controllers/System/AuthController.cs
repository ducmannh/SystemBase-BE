using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SystemBase.BE.DTOs.Auth;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Services.Auth;

namespace SystemBase.BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            SetTokenCookie(result.Data!.Token);
            SetRefreshTokenCookie(result.Data.RefreshToken);
            result.Data.Token = string.Empty;
            result.Data.RefreshToken = string.Empty;

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = ParseUserAgent(Request.Headers["User-Agent"].ToString());
            var result = await _authService.LoginAsync(request, ipAddress, userAgent);
            
            if (!result.IsSuccess)
            {
                return Unauthorized(result);
            }
            
            SetTokenCookie(result.Data!.Token);
            SetRefreshTokenCookie(result.Data.RefreshToken);
            result.Data.Token = string.Empty;
            result.Data.RefreshToken = string.Empty;

            return Ok(result);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwtToken");
            Response.Cookies.Delete("refreshToken");
            return Ok(new RestData<object> { IsSuccess = true, Message = "Đăng xuất thành công." });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new RestData<object> { IsSuccess = false, Message = "Không tìm thấy Refresh Token." });
            }

            var result = await _authService.RefreshAsync(refreshToken);
            
            if (!result.IsSuccess)
            {
                return Unauthorized(result);
            }

            SetTokenCookie(result.Data!.Token);
            SetRefreshTokenCookie(result.Data.RefreshToken);
            
            result.Data.Token = string.Empty;
            result.Data.RefreshToken = string.Empty;

            return Ok(result);
        }

        private void SetTokenCookie(string token)
        {
            var isHttps = Request.IsHttps || Request.Headers["X-Forwarded-Proto"] == "https";
            var expirationInMinutes = _configuration.GetValue<int>("JwtSettings:ExpirationInMinutes");
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes),
                SameSite = SameSiteMode.Lax,
                Secure = isHttps
            };
            Response.Cookies.Append("jwtToken", token, cookieOptions);
        }

        private void SetRefreshTokenCookie(string token)
        {
            var isHttps = Request.IsHttps || Request.Headers["X-Forwarded-Proto"] == "https";
            var expirationInDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationInDays");
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(expirationInDays),
                SameSite = SameSiteMode.Lax,
                Secure = isHttps
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string GetClientIpAddress()
        {
            var ip = Request.Headers["X-Forwarded-For"].ToString();
            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            else
            {
                ip = ip.Split(',')[0].Trim();
            }
            
            if (ip == "::1")
            {
                ip = "127.0.0.1";
            }

            return string.IsNullOrEmpty(ip) ? "Unknown" : ip;
        }

        private string ParseUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";
            
            string browser = "Unknown Browser";
            string os = "Unknown OS";

            if (userAgent.Contains("Edg/")) browser = "Edge";
            else if (userAgent.Contains("Chrome/")) browser = "Chrome";
            else if (userAgent.Contains("Firefox/")) browser = "Firefox";
            else if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/")) browser = "Safari";
            else if (userAgent.Contains("OPR/") || userAgent.Contains("Opera/")) browser = "Opera";

            if (userAgent.Contains("Windows NT 10.0")) os = "Windows 10/11";
            else if (userAgent.Contains("Windows NT 6.3")) os = "Windows 8.1";
            else if (userAgent.Contains("Windows NT 6.2")) os = "Windows 8";
            else if (userAgent.Contains("Windows NT 6.1")) os = "Windows 7";
            else if (userAgent.Contains("Mac OS X")) os = "MacOS";
            else if (userAgent.Contains("Android")) os = "Android";
            else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) os = "iOS";
            else if (userAgent.Contains("Linux")) os = "Linux";

            return $"{browser} - {os}";
        }

        [HttpPost("force-change-password")]
        public async Task<IActionResult> ForceChangePassword(ForceChangePasswordRequestDto request)
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = ParseUserAgent(Request.Headers["User-Agent"].ToString());

            var result = await _authService.ForceChangePasswordAsync(request, ipAddress, userAgent);
            if (result.IsSuccess && result.Data != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Cần cấu hình false nếu test HTTP localhost
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"]))
                };
                Response.Cookies.Append("jwtToken", result.Data.Token, cookieOptions);
            }
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequestDto request)
        {
            var result = await _authService.VerifyResetCodeAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
