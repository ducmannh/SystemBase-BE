using Microsoft.Extensions.DependencyInjection;
using SystemBase.BE.Services.Auth;
using SystemBase.BE.Services.SystemFunction;
using SystemBase.BE.Services.SystemRole;
using SystemBase.BE.Services.LoginLog;
using SystemBase.BE.Services.ActionLog;
using SystemBase.BE.Services.System.SystemSecuritySetting;
using SystemBase.BE.Services.User;
using SystemBase.BE.Services.ErrorLog;
using SystemBase.BE.Services.System.Email;
using SystemBase.BE.Infrastructure.Dapper;

namespace SystemBase.BE
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Cấu hình Dapper Connection Factory
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            // Đăng ký Service vào DI container
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISystemFunctionService, SystemFunctionService>();
            services.AddScoped<ILoginLogService, LoginLogService>();
            services.AddScoped<ISystemRoleService, SystemRoleService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IActionLogService, ActionLogService>();
            services.AddScoped<IErrorLogService, ErrorLogService>();
            services.AddScoped<ISystemSecuritySettingService, SystemSecuritySettingService>();
            services.AddHttpContextAccessor();

            return services;
        }
    }
}
