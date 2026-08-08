namespace Application.Features.CentralDbSync.Models;

public static class BootstrapRequestStatus
{
    public const string PendingEnqueue = "pending_enqueue";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string WaitingForLock = "waiting_for_lock";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

public static class BootstrapRequestType
{
    /// <summary>
    /// Current in-memory bootstrap flow — reads all rows and applies directly.
    /// </summary>
    public const string InMemory = "in_memory";

    /// <summary>
    /// Scalable parent-child bootstrap flow with dynamic staging and sequential child jobs.
    /// </summary>
    public const string Scalable = "scalable";
}

public sealed record BootstrapRequest
{
    public Guid RequestId { get; init; }
    public required string SourceTable { get; init; }
    public required string Status { get; init; }
    public string BootstrapType { get; init; } = BootstrapRequestType.InMemory;
    public string? HangfireJobId { get; init; }
    public long RowsStaged { get; init; }
    public long? TotalRowsExpected { get; init; }
    public int AttemptCount { get; init; }
    public int ReconcileAttemptCount { get; init; }
    public int ScheduleFailureCount { get; init; }
    public DateTime? NextReconcileAt { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public DateTime? FirstRecoveryAt { get; init; }
    public DateTime? LastRecoveryAt { get; init; }
    public string? ReconcileClaimToken { get; init; }
    public DateTime? ReconcileClaimedAt { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static BootstrapRequest New(string sourceTable) => new()
    {
        RequestId = Guid.NewGuid(),
        SourceTable = sourceTable,
        Status = BootstrapRequestStatus.PendingEnqueue,
        RequestedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static BootstrapRequest NewScalable(string sourceTable) => new()
    {
        RequestId = Guid.NewGuid(),
        SourceTable = sourceTable,
        Status = BootstrapRequestStatus.PendingEnqueue,
        BootstrapType = BootstrapRequestType.Scalable,
        RequestedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
