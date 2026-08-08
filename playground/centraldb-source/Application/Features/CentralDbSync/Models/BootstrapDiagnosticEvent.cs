namespace Application.Features.CentralDbSync.Models;

public sealed record BootstrapDiagnosticEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public Guid RequestId { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? ChildId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string? FromStatus { get; init; }
    public string? ToStatus { get; init; }
    public string? HangfireJobId { get; init; }
    public string? FencingTokenHash { get; init; }
    public int? ChildSequence { get; init; }
    public long? RowsAffected { get; init; }
    public string DiagnosticCode { get; init; } = string.Empty;
    public string? SanitizedMessage { get; init; }
    public string InitiatedBy { get; init; } = "system";
    public long SequenceNo { get; init; }

    public static BootstrapDiagnosticEvent Create(
        Guid requestId, Guid? parentId, Guid? childId,
        string entityType, string eventType,
        string? fromStatus, string? toStatus,
        string? hangfireJobId, string? fencingToken,
        int? childSequence, long? rowsAffected,
        string diagnosticCode, string? message,
        string initiatedBy)
    {
        return new BootstrapDiagnosticEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            RequestId = requestId,
            ParentId = parentId,
            ChildId = childId,
            EntityType = entityType,
            EventType = eventType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            HangfireJobId = hangfireJobId,
            FencingTokenHash = fencingToken is not null
                ? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(fencingToken)))[..12]
                : null,
            ChildSequence = childSequence,
            RowsAffected = rowsAffected,
            DiagnosticCode = diagnosticCode,
            SanitizedMessage = BootstrapDiagnosticSanitizer.Sanitize(message),
            InitiatedBy = initiatedBy
        };
    }
}

public static class BootstrapDiagnosticEventType
{
    public const string RequestCreated = "request_created";
    public const string ParentCreated = "parent_created";
    public const string ChildCreated = "child_created";
    public const string ParentClaimed = "parent_claimed";
    public const string ChildClaimed = "child_claimed";
    public const string C0Captured = "c0_captured";
    public const string StagingCreated = "staging_created";
    public const string ChildCompleted = "child_completed";
    public const string PhaseClaimed = "phase_claimed";
    public const string FinalizeStarted = "finalize_started";
    public const string CtCatchUpStarted = "ct_catch_up_started";
    public const string PublishStarted = "publish_started";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string RecoveryProbe = "recovery_probe";
    public const string RecoveryScheduled = "recovery_scheduled";
    public const string ScheduleFailure = "schedule_failure";
    public const string RetryRequested = "retry_requested";
    public const string ReconcileRequested = "reconcile_requested";
    public const string CancelRequested = "cancel_requested";
    public const string CancellationObserved = "cancellation_observed";
    public const string CleanupStarted = "cleanup_started";
    public const string CleanupCompleted = "cleanup_completed";
    public const string CleanupFailed = "cleanup_failed";
    public const string Cancelled = "cancelled";
    public const string RetentionPruned = "retention_pruned";
}

public static class BootstrapDiagnosticEntityType
{
    public const string Request = "request";
    public const string Parent = "parent";
    public const string Child = "child";
}
