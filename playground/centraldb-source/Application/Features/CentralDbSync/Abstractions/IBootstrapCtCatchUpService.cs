using Application.Features.CentralDbSync.Mapping;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Applies CT changes from C0 to C1 into the dynamic staging table after all
/// children have completed. Handles inserts, updates, deletes, and filter exclusions.
/// </summary>
public interface IBootstrapCtCatchUpService
{
    /// <summary>
    /// Reads CT changes in (C0, C1] and applies them to the dynamic staging table.
    /// <paramref name="stagingSchema"/> and <paramref name="stagingTableName"/> identify
    /// the per-parent staging table created during child execution.
    /// Returns the number of changes applied (inserts+updates+deletes).
    /// Throws <see cref="CheckpointInvalidException"/> if CT history expired.
    /// </summary>
    Task<CtCatchUpResult> CatchUpAsync(
        TableMappingRule rule,
        long baselineVersion,
        long watermarkVersion,
        string stagingSchema,
        string stagingTableName,
        CancellationToken ct);
}

/// <summary>
/// Result of CT catch-up operation.
/// </summary>
public sealed record CtCatchUpResult
{
    public bool IsValid { get; init; }
    public int ChangesApplied { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static CtCatchUpResult Success(int changesApplied) => new()
    {
        IsValid = true,
        ChangesApplied = changesApplied
    };

    public static CtCatchUpResult Fail(string code, string message) => new()
    {
        IsValid = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
