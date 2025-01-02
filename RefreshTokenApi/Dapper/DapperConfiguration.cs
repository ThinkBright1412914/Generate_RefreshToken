using Microsoft.Data.SqlClient;
using System.Data;

namespace RefreshTokenApi.Dapper
{
    public class DapperConfiguration : IDapperConfiguration , IDisposable
    {
        private IDbConnection _connection;
        private IDbTransaction _dbTransaction;
        private static IConfiguration _configuration;

        public static void SetConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DapperConfiguration()
        {
            ConnectionString = _configuration.GetConnectionString("RefreshToken");
        }

        public string ConnectionString { get; set; }

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null || _connection.State == ConnectionState.Closed)
                {
                    _connection = new SqlConnection(ConnectionString);
                    _connection.Open();
                }
                return _connection;
            }
        }
        public IDbTransaction Transaction => _dbTransaction;

        public void Dispose()
        {
            _connection?.Dispose();
            _dbTransaction?.Dispose();
        }
    }

    public interface IDapperConfiguration
    {
        string ConnectionString { get; }
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
    }
}
