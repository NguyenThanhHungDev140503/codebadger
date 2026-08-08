using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// CAS-oriented store for bootstrap_parent lifecycle.
/// Every update method includes parent ID, fencing token, and expected current status
/// in its WHERE clause. A zero-row update means the worker is stale and must stop.
/// </summary>
public interface IBootstrapParentStore
{
    /// <summary>Creates a new parent in PendingEnqueue status.</summary>
    Task<BootstrapParent> CreateAsync(
        string ruleName, string sourceTable, string targetSchema,
        string targetTable, string stagingTableName,
        Guid? bootstrapRequestId, CancellationToken ct);

    /// <summary>Returns parent by ID, or null if not found.</summary>
    Task<BootstrapParent?> GetAsync(Guid parentId, CancellationToken ct);

    /// <summary>Returns the most recent parent for a rule, or null.</summary>
    Task<BootstrapParent?> GetByRuleNameAsync(string ruleName, CancellationToken ct);

    /// <summary>CAS-claims the parent row by fencing token. Returns false if stale.</summary>
    Task<bool> TryClaimAsync(Guid parentId, Guid fencingToken, CancellationToken ct);

    /// <summary>Transition from one status to another, guarded by fencing token.</summary>
    Task<bool> TryTransitionAsync(Guid parentId, Guid fencingToken,
        string fromStatus, string toStatus, CancellationToken ct);

    /// <summary>Updates last_heartbeat_at. Returns false if fencing token mismatch.</summary>
    Task<bool> HeartbeatAsync(Guid parentId, Guid fencingToken, CancellationToken ct);

    /// <summary>Sets BaselineVersion when C0 is captured.</summary>
    Task<bool> SetBaselineVersionAsync(Guid parentId, Guid fencingToken,
        long baselineVersion, CancellationToken ct);

    /// <summary>Sets StagingCreatedAt = NOW() after dynamic CREATE TABLE succeeds.</summary>
    Task<bool> SetStagingCreatedAsync(Guid parentId, Guid fencingToken, CancellationToken ct);

    /// <summary>Updates cursor, row counters, and TotalRowsExpected after a child completes.</summary>
    Task<bool> UpdateProgressAsync(Guid parentId, Guid fencingToken,
        string? lastProcessedKey, long rowsStaged, long? totalRowsExpected, CancellationToken ct);

    /// <summary>Marks the parent as CatchingUp with the captured watermark.</summary>
    Task<bool> MarkCtCatchUpAsync(Guid parentId, Guid fencingToken,
        long watermark, CancellationToken ct);

    /// <summary>
    /// Returns parents in active states (PendingEnqueue, Running, CatchingUp, Publishing)
    /// whose last_heartbeat_at is older than the cutoff. Used by the recovery job.
    /// </summary>
    Task<IReadOnlyList<BootstrapParent>> GetStaleParentsAsync(DateTime cutoffUtc, CancellationToken ct);

    /// <summary>
    /// Returns terminal parents eligible for orphan stage cleanup:
    /// - Failed/RecoveryPending with last_heartbeat_at &lt; cutoff AND staging_created_at IS NOT NULL
    /// - Expired with cleanup_completed_at IS NULL
    /// </summary>
    Task<IReadOnlyList<BootstrapParent>> GetCleanupCandidatesAsync(DateTime cutoffUtc, CancellationToken ct);

    /// <summary>Sets cleanup_completed_at = NOW() after successful DROP TABLE.</summary>
    Task<bool> SetCleanupCompletedAsync(Guid parentId, CancellationToken ct);

    /// <summary>Sets DeferredCtPending = true. Returns false if already set.</summary>
    Task<bool> TrySetDeferredCtAsync(Guid parentId, CancellationToken ct);

    /// <summary>
    /// Claims durable Hangfire ownership for the current finalization phase. Only the
    /// matching parent/fencing/phase/job snapshot may acquire or take over a stale claim.
    /// </summary>
    Task<bool> TryClaimPhaseJobAsync(Guid parentId, Guid fencingToken, string expectedStatus,
        string? expectedJobId, string claimToken, DateTime staleClaimBeforeUtc, CancellationToken ct);

    /// <summary>Finalizes a phase claim with the actual Hangfire job id.</summary>
    Task<bool> TryFinalizePhaseJobAsync(Guid parentId, Guid fencingToken, string expectedStatus,
        string claimToken, string actualJobId, string phaseKind, CancellationToken ct);

    /// <summary>
    /// Transitions the parent to Failed regardless of current active status.
    /// Does not require a specific fromStatus — accepts any active status.
    /// Sets error_code and error_message. Returns false if fencing token does not match.
    /// </summary>
    Task<bool> TryFailAsync(Guid parentId, Guid fencingToken,
        string errorCode, string errorMessage, CancellationToken ct);

    /// <summary>
    /// Fails the parent phase claim using the parent's own CAS (fencing token, status,
    /// phase_claim_token). Persists error evidence on the parent row. Does not use
    /// the request claim token.
    /// </summary>
    Task<bool> TryFailPhaseClaimAsync(Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken,
        string errorCode, string errorMessage, CancellationToken ct);

    /// <summary>Persists scheduling evidence without releasing an owned phase claim.</summary>
    Task<bool> TryRecordPhaseClaimSchedulingFailureAsync(Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken, string errorCode, string errorMessage,
        CancellationToken ct);

    /// <summary>
    /// CAS-transitions the parent from an active status to cancel_requested.
    /// Sets cancel_requested_at and cancel_requested_by.
    /// Only allows cancellation of Running, CatchingUp, Publishing parents.
    /// </summary>
    Task<bool> TryRequestCancelAsync(Guid parentId, Guid fencingToken,
        string initiatedBy, CancellationToken ct);

    /// <summary>
    /// Transitions the parent from cancel_requested to cancelled after cleanup.
    /// CAS-guarded: only succeeds when current status is cancel_requested.
    /// </summary>
    Task<bool> TryMarkCancelledAsync(Guid parentId, CancellationToken ct);
}
