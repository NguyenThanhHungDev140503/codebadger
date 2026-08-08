namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Npgsql;
using System.Data.Common;

/// <summary>
/// Creates real NpgsqlConnection instances for the Central DB (PostgreSQL).
/// Used in production; mocked in unit tests.
/// </summary>
public sealed class NpgsqlConnectionFactory : ICentralDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
        => _connectionString = connectionString;

    public DbConnection CreateConnection()
        => new NpgsqlConnection(_connectionString);
}
