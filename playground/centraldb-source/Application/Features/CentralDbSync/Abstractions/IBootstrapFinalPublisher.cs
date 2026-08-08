using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Executes the atomic final publish: set-based upsert from staging to target,
/// CAS checkpoint advance to C1, run log, parent completion, CT outbox marker,
/// and DROP TABLE for the staging table — all in one PostgreSQL transaction.
/// </summary>
public interface IBootstrapFinalPublisher
{
    /// <summary>
    /// Publishes staging data to the rule's target table in one atomic transaction.
    /// </summary>
    /// <param name="rule">The mapping rule defining target schema/table.</param>
    /// <param name="parentId">The parent ID for run log and completion.</param>
    /// <param name="fencingToken">Fencing token for CAS guard on parent completion.</param>
    /// <param name="stagingSchema">The staging schema (sync_meta).</param>
    /// <param name="stagingTableName">The dynamic staging table name.</param>
    /// <param name="baselineVersion">C0 — used for CAS checkpoint advance.</param>
    /// <param name="watermarkVersion">C1 — the new checkpoint version.</param>
    /// <param name="bootstrapRequestId">Optional bootstrap request ID to mark completed in the same transaction.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<FinalPublishResult> PublishAsync(
        TableMappingRule rule,
        Guid parentId,
        Guid fencingToken,
        string stagingSchema,
        string stagingTableName,
        long baselineVersion,
        long watermarkVersion,
        Guid? bootstrapRequestId,
        CancellationToken ct);
}

/// <summary>
/// Result of the final publish operation.
/// </summary>
public sealed record FinalPublishResult
{
    public bool IsSuccess { get; init; }
    public int RowsUpserted { get; init; }
    public int RowsDeactivated { get; init; }
    public int RowsDeleted { get; init; }
    public long CheckpointAfter { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static FinalPublishResult Success(int upserted, int deactivated, int deleted,
        long checkpointAfter) => new()
    {
        IsSuccess = true,
        RowsUpserted = upserted,
        RowsDeactivated = deactivated,
        RowsDeleted = deleted,
        CheckpointAfter = checkpointAfter
    };

    public static FinalPublishResult Fail(string code, string message) => new()
    {
        IsSuccess = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
