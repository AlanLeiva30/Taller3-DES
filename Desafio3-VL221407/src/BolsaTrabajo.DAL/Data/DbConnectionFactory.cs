using System.Data;
using Microsoft.Data.SqlClient;

namespace BolsaTrabajo.DAL.Data;

/// <summary>Crea conexiones a SQL Server para las consultas hechas con Dapper.</summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
