namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Durable bootstrap parent state for the scalable parent-child flow.
/// One parent per rule_name; owns dynamic staging lifecycle and CT catch-up.
/// </summary>
public sealed record BootstrapParent
{
    public Guid ParentId { get; init; }
    public required string RuleName { get; init; }
    public required string SourceTable { get; init; }
    public required string TargetSchema { get; init; }
    public required string TargetTable { get; init; }
    public required string Status { get; init; }
    public Guid FencingToken { get; init; }

    /// <summary>CT baseline captured before children start.</summary>
    public long? BaselineVersion { get; init; }

    /// <summary>CT watermark captured before final catch-up.</summary>
    public long? WatermarkVersion { get; init; }

    /// <summary>Last primary key from the most recent completed child.</summary>
    public string? LastProcessedKey { get; init; }

    public long RowsStaged { get; init; }
    public long? TotalRowsExpected { get; init; }
    public int AttemptCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastHeartbeatAt { get; init; }
    public DateTime? CompletedAt { get; init; }

    // Dynamic staging identity
    public string StagingSchema { get; init; } = "sync_meta";
    public required string StagingTableName { get; init; }
    public DateTime? StagingCreatedAt { get; init; }
    public DateTime? CleanupCompletedAt { get; init; }

    // Deferred CT state
    public bool DeferredCtPending { get; init; }

    // Link to originating bootstrap_request
    public Guid? BootstrapRequestId { get; init; }

    // Durable Hangfire ownership for CatchingUp/Publishing recovery.
    public string? PhaseJobId { get; init; }
    public string? PhaseJobKind { get; init; }
    public string? PhaseClaimToken { get; init; }
    public DateTime? PhaseClaimedAt { get; init; }
    public int PhaseScheduleFailureCount { get; init; }
    public DateTime? PhaseNextReconcileAt { get; init; }

    // Error fields
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    // Cancellation fields
    public DateTime? CancelRequestedAt { get; init; }
    public string? CancelRequestedBy { get; init; }
}
