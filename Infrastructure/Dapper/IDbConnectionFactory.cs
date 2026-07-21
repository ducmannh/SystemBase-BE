using System.Data;

namespace SystemBase.BE.Infrastructure.Dapper
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
