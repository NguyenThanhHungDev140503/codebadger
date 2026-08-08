namespace Application.Features.CentralDbSync.Abstractions;

using System.Data.Common;

/// <summary>
/// Factory for creating database connections to the Central DB (PostgreSQL).
/// Enables unit testing by allowing the connection to be mocked.
/// </summary>
public interface ICentralDbConnectionFactory
{
    DbConnection CreateConnection();
}
