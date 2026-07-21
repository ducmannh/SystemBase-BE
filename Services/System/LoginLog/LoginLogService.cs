using System;
using System.Linq;
using System.Threading.Tasks;
using SystemBase.BE.DTOs.Shared;
using SystemBase.BE.DTOs.LoginLog;
using Dapper;
using Dapper.FastCrud;
using SystemBase.BE.Infrastructure.Dapper;

namespace SystemBase.BE.Services.LoginLog
{
    public class LoginLogService : ILoginLogService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LoginLogService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<RestData<PagedResult<LoginLogDto>>> GetPagedAsync(PaginationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            var whereClause = "1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                var keyword = request.Keyword.ToLower();
                whereClause += " AND (LOWER(UserName) LIKE @Keyword OR LOWER(IpAddress) LIKE @Keyword OR LOWER(Status) LIKE @Keyword OR LOWER(UserAgent) LIKE @Keyword OR LOWER(Message) LIKE @Keyword)";
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

            var totalCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM LoginLogs WHERE {whereClause}", parameters);

            if (totalCount > 0 && (request.PageIndex - 1) * request.PageSize >= totalCount)
            {
                request.PageIndex = 1;
            }

            var skip = (request.PageIndex - 1) * request.PageSize;
            parameters.Add("Skip", skip);
            parameters.Add("Take", request.PageSize);

            var sql = $"SELECT * FROM LoginLogs WHERE {whereClause} ORDER BY CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var items = await connection.QueryAsync<Models.SystemManagement.LoginLog>(sql, parameters);

            var dtos = items.Select(x => new LoginLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.UserName,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                Status = x.Status,
                Message = x.Message,
                CreatedAt = x.CreatedAt
            }).ToList();

            return new RestData<PagedResult<LoginLogDto>>
            {
                IsSuccess = true,
                Data = new PagedResult<LoginLogDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize
                }
            };
        }

        public async Task<RestData<object>> DeleteAsync(global::System.Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var deleted = await connection.ExecuteAsync("DELETE FROM LoginLogs WHERE Id = @Id", new { Id = id });
            
            if (deleted == 0) return new RestData<object> { IsSuccess = false, Message = "Bản ghi không tồn tại." };

            return new RestData<object> { IsSuccess = true, Message = "Xóa thành công." };
        }

        public async Task<RestData<object>> DeleteManyAsync(global::System.Collections.Generic.List<global::System.Guid> ids)
        {
            using var connection = _connectionFactory.CreateConnection();
            var idsString = string.Join("','", ids);
            var query = $"DELETE FROM LoginLogs WHERE Id IN ('{idsString}')";
            var deleted = await connection.ExecuteAsync(query);
            
            if (deleted == 0) return new RestData<object> { IsSuccess = false, Message = "Không tìm thấy bản ghi nào để xóa." };

            return new RestData<object> { IsSuccess = true, Message = "Xóa các bản ghi thành công." };
        }
    }
}
