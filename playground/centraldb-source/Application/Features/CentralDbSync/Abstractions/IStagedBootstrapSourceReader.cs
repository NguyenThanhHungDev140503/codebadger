using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Result of scalable bootstrap preflight validation.
/// </summary>
public sealed record BootstrapSourcePreflight
{
    /// <summary>CT baseline version (C0) captured before children start.</summary>
    public long BaselineVersion { get; init; }

    /// <summary>Total rows for display purposes (approximate).</summary>
    public long? TotalRows { get; init; }

    /// <summary>Whether the preflight passed.</summary>
    public bool IsValid { get; init; }

    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static BootstrapSourcePreflight Valid(long baselineVersion, long? totalRows) => new()
    {
        BaselineVersion = baselineVersion,
        TotalRows = totalRows,
        IsValid = true
    };

    public static BootstrapSourcePreflight Fail(string code, string message) => new()
    {
        IsValid = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

/// <summary>
/// Short-lived keyset-based reader for child bootstrap batches.
/// Each call opens and closes its own ReadCommitted connection.
/// </summary>
public interface IStagedBootstrapSourceReader
{
    /// <summary>
    /// Validates CT is enabled on the source table, captures C0 (baseline version),
    /// and returns an approximate total row count. Called once per parent.
    /// </summary>
    Task<BootstrapSourcePreflight> ValidateAndCaptureBaselineAsync(
        TableMappingRule rule, CancellationToken ct);

    /// <summary>
    /// Reads the next batch of up to <paramref name="batchSize"/> rows using keyset
    /// pagination from the given <paramref name="afterKey"/> (null for first batch).
    /// Returns empty list when no more rows are available (EOF).
    /// </summary>
    Task<IReadOnlyList<GenericSourceRow>> ReadBatchAsync(
        TableMappingRule rule, object? afterKey, int batchSize, CancellationToken ct);

    /// <summary>
    /// Returns the current CT version (CHANGE_TRACKING_CURRENT_VERSION()) for
    /// watermark capture.
    /// </summary>
    Task<long> GetCurrentVersionAsync(CancellationToken ct);

    /// <summary>
    /// Reads CT changes from SQL Server for the range (baselineVersion, watermarkVersion]
    /// using CHANGETABLE(CHANGES ...). Returns separate lists for upserts (I/U) and
    /// deleted primary key values (D).
    /// </summary>
    Task<CtDeltaResult> ReadCtDeltaAsync(
        TableMappingRule rule, long baselineVersion, long watermarkVersion, CancellationToken ct);
}

/// <summary>
/// Result of a CT delta read: rows to upsert and primary key values to delete.
/// </summary>
public sealed record CtDeltaResult
{
    public IReadOnlyList<GenericSourceRow> Upserts { get; init; } = [];
    public IReadOnlyList<object?[]> DeletedPrimaryKeys { get; init; } = [];
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsValid => ErrorCode is null;

    public static CtDeltaResult Success(
        IReadOnlyList<GenericSourceRow> upserts,
        IReadOnlyList<object?[]> deletedPks) => new()
    {
        Upserts = upserts,
        DeletedPrimaryKeys = deletedPks
    };

    public static CtDeltaResult Fail(string code, string message) => new()
    {
        ErrorCode = code,
        ErrorMessage = message
    };
}
