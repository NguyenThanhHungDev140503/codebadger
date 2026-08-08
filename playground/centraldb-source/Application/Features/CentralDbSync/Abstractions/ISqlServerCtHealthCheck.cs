namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Diagnostics: checks whether Change Tracking is enabled at the SQL Server level
/// for a specific source table, independent of the application config.
/// </summary>
public interface ISqlServerCtHealthCheck
{
    /// <summary>
    /// Returns CT status for the given source table.
    /// </summary>
    Task<CtHealthResult> CheckAsync(string sourceTable, CancellationToken ct);
}

/// <summary>
/// Result of a Change Tracking health check for one table.
/// </summary>
public sealed record CtHealthResult
{
    public required string SourceTable { get; init; }
    public required bool IsCtEnabled { get; init; }
    public string? SchemaQualifiedName { get; init; }
    public long? CurrentVersion { get; init; }
    public long? MinValidVersion { get; init; }
    public string? ErrorMessage { get; init; }
}
