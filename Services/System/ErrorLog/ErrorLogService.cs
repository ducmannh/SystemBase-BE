using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.ErrorLog;
using SystemBase.BE.Models.SystemManagement;
using System.Security.Claims;
using SystemBase.BE.Helpers;
using Dapper;
using Dapper.FastCrud;
using SystemBase.BE.Infrastructure.Dapper;

namespace SystemBase.BE.Services.ErrorLog
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ErrorLogService(IDbConnectionFactory connectionFactory, IHttpContextAccessor httpContextAccessor)
        {
            _connectionFactory = connectionFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(Exception exception, string path)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdString = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = user?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            Guid? userId = null;
            if (Guid.TryParse(userIdString, out var parsedId))
            {
                userId = parsedId;
            }

            var ip = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].ToString();
            if (string.IsNullOrEmpty(ip))
            {
                ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            }
            else
            {
                ip = ip.Split(',')[0].Trim();
            }
            if (ip == "::1") ip = "127.0.0.1";
            if (string.IsNullOrEmpty(ip)) ip = "Unknown";

            using var connection = _connectionFactory.CreateConnection();
            var log = new Models.SystemManagement.ErrorLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? string.Empty,
                Path = path,
                IpAddress = ip,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            await connection.InsertAsync(log);
        }

        public async Task<RestData<PagedResult<ErrorLogDto>>> GetPagedAsync(PaginationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            var whereClause = "1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                var keyword = request.Keyword.ToLower();
                whereClause += " AND (LOWER(UserName) LIKE @Keyword OR LOWER(Message) LIKE @Keyword OR LOWER(Path) LIKE @Keyword)";
                parameters.Add("Keyword", $"%{keyword}%");
            }

            if (request.StartDate.HasValue)
            {
                whereClause += " AND CreatedAt >= @StartDate";
                parameters.Add("StartDate", request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                whereClause += " AND CreatedAt <= @EndDate";
                parameters.Add("EndDate", endDate);
            }

            var totalCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM ErrorLogs WHERE {whereClause}", parameters);

            if (totalCount > 0 && (request.PageIndex - 1) * request.PageSize >= totalCount)
            {
                request.PageIndex = 1;
            }

            var skip = (request.PageIndex - 1) * request.PageSize;
            parameters.Add("Skip", skip);
            parameters.Add("Take", request.PageSize);

            var sql = $"SELECT * FROM ErrorLogs WHERE {whereClause} ORDER BY CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var items = await connection.QueryAsync<Models.SystemManagement.ErrorLog>(sql, parameters);

            var dtos = items.Select(x => new ErrorLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.UserName,
                Message = x.Message,
                StackTrace = x.StackTrace,
                Path = x.Path,
                IpAddress = x.IpAddress,
                CreatedAt = x.CreatedAt
            }).ToList();

            return new RestData<PagedResult<ErrorLogDto>>
            {
                IsSuccess = true,
                Data = new PagedResult<ErrorLogDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize
                }
            };
        }

        public async Task<RestData<object>> DeleteAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var deleted = await connection.ExecuteAsync("DELETE FROM ErrorLogs WHERE Id = @Id", new { Id = id });
            
            if (deleted == 0) return new RestData<object> { IsSuccess = false, Message = "Không tìm thấy nhật ký" };

            return new RestData<object> { IsSuccess = true, Message = "Xóa nhật ký lỗi thành công" };
        }

        public async Task<RestData<object>> DeleteManyAsync(global::System.Collections.Generic.List<Guid> ids)
        {
            using var connection = _connectionFactory.CreateConnection();
            var idsString = string.Join("','", ids);
            var query = $"DELETE FROM ErrorLogs WHERE Id IN ('{idsString}')";
            var deleted = await connection.ExecuteAsync(query);
            
            if (deleted == 0) return new RestData<object> { IsSuccess = false, Message = "Không có nhật ký nào được chọn" };

            return new RestData<object> { IsSuccess = true, Message = $"Xóa {deleted} nhật ký lỗi thành công" };
        }
    }
}
