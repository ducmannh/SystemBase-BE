using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.Services.ErrorLog;

namespace SystemBase.BE.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            using (var scope = context.RequestServices.CreateScope())
            {
                var errorLogService = scope.ServiceProvider.GetRequiredService<IErrorLogService>();
                await errorLogService.LogAsync(exception, context.Request.Path);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new RestData<object>
            {
                IsSuccess = false,
                Message = "Đã có lỗi không mong muốn xảy ra trong hệ thống. Kiểm tra Nhật ký lỗi."
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        }
    }
}
