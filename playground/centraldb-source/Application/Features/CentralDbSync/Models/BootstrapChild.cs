namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Durable child state for the scalable parent-child bootstrap flow.
/// Each child reads at most 10,000 rows and stages them via COPY.
/// Children are created lazily; only the next uncompleted child can be claimed.
/// </summary>
public sealed record BootstrapChild
{
    public Guid ChildId { get; init; }
    public Guid ParentId { get; init; }
    public int Sequence { get; init; }
    public string? AfterKey { get; init; }
    public string? LastKey { get; init; }
    public long RowsRead { get; init; }
    public required string Status { get; init; }
    public int AttemptCount { get; init; }
    public int ReconcileAttemptCount { get; init; }
    public int ScheduleFailureCount { get; init; }
    public DateTime? NextReconcileAt { get; init; }
    public string? HangfireJobId { get; init; }
    public string? ReconcileClaimToken { get; init; }
    public DateTime? ReconcileClaimedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastHeartbeatAt { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
