using System.Threading.Tasks;
using SystemBase.BE.DTOs.Auth;
using SystemBase.BE.DTOs.Shared;

namespace SystemBase.BE.Services.Auth
{
    public interface IAuthService
    {
        Task<RestData<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<RestData<AuthResponseDto>> LoginAsync(LoginRequestDto request, string ipAddress, string userAgent);
        Task<RestData<AuthResponseDto>> RefreshAsync(string refreshToken);
        Task<RestData<AuthResponseDto>> ForceChangePasswordAsync(ForceChangePasswordRequestDto request, string ipAddress, string userAgent);
        Task<RestData<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<RestData<string>> VerifyResetCodeAsync(VerifyResetCodeRequestDto request);
        Task<RestData<string>> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}
