using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

public interface IBootstrapRequestStore
{
    Task<BootstrapRequestResult> CreateOrGetActiveAsync(string sourceTable, CancellationToken ct,
        string bootstrapType = "in_memory");
    Task<BootstrapRequest?> GetAsync(Guid requestId, CancellationToken ct);
    [Obsolete("Use the snapshot-guarded Try* transition contracts.")]
    Task<bool> MarkQueuedAsync(Guid requestId, string hangfireJobId, CancellationToken ct);
    [Obsolete("Use the snapshot-guarded Try* transition contracts.")]
    Task<bool> TryMarkRunningAsync(Guid requestId, CancellationToken ct);
    [Obsolete("Use the snapshot-guarded Try* transition contracts.")]
    Task MarkWaitingForLockAsync(Guid requestId, string message, CancellationToken ct);
    [Obsolete("Use the snapshot-guarded Try* transition contracts.")]
    Task MarkCompletedAsync(Guid requestId, CancellationToken ct);
    [Obsolete("Use the snapshot-guarded Try* transition contracts.")]
    Task MarkFailedAsync(Guid requestId, string code, string message, CancellationToken ct);

    Task<bool> TryMarkQueuedAsync(Guid requestId, string expectedStatus, string expectedClaimToken,
        string hangfireJobId, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement snapshot-guarded transitions.");
    Task<bool> TryMarkRunningAsync(Guid requestId, string expectedStatus, string expectedJobId,
        CancellationToken ct) =>
        throw new NotSupportedException("The store must implement snapshot-guarded transitions.");
    Task<bool> TryMarkWaitingForLockAsync(Guid requestId, string expectedStatus, string expectedJobId,
        string message, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement snapshot-guarded transitions.");
    Task<bool> TryCompleteAsync(Guid requestId, string expectedStatus, string expectedJobId,
        CancellationToken ct) =>
        throw new NotSupportedException("The store must implement snapshot-guarded transitions.");
    Task<bool> TryFailAsync(Guid requestId, string expectedStatus, string expectedJobId,
        string code, string message, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement snapshot-guarded transitions.");

    Task<bool> TryRecordSchedulingFailureAsync(Guid requestId, string expectedStatus,
        string expectedJobId, string? claimToken, string code, string message, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement snapshot-guarded scheduling failure.");

    /// <summary>
    /// Atomically records a scalable phase scheduling failure on both the request and
    /// its parent. The third failure terminalizes both ownership records.
    /// </summary>
    Task<bool> TryRecordScalablePhaseSchedulingFailureAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string code, string message, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement atomic phase scheduling failure.");

    Task<IReadOnlyList<BootstrapRequest>> GetPendingEnqueueBeforeAsync(DateTime cutoffUtc, CancellationToken ct);

    /// <summary>
    /// Returns requests still stuck in <c>queued</c> status whose <c>updated_at</c> is
    /// at or before <paramref name="cutoffUtc"/>. Uses <c>updated_at</c> (not
    /// <c>requested_at</c>) so that freshly recovered requests are not re-scanned
    /// before their CentralDbSyncOptions.BootstrapIdleReconciliationAfter idle window elapses.
    /// </summary>
    Task<IReadOnlyList<BootstrapRequest>> GetQueuedBeforeAsync(DateTime cutoffUtc, CancellationToken ct);

    /// <summary>
    /// Atomically claims a pending or recovery slot using the full durable snapshot.
    /// A missing claim or one acquired at or before <paramref name="staleClaimBeforeUtc"/>
    /// may be claimed; the caller owns the token only when this returns <c>true</c>.
    /// </summary>
    Task<bool> TryClaimSlotAsync(
        BootstrapRecoveryExpectation expectation,
        string claimToken,
        DateTime staleClaimBeforeUtc,
        bool isRecovery,
        CancellationToken ct);

    /// <summary>
    /// Atomically persists the real Hangfire job id, clears the matching claim token,
    /// and returns the request to Queued. The full original snapshot guards the CAS.
    /// Recovery finalization increments recovery accounting exactly once.
    /// </summary>
    Task<bool> TryFinalizeClaimAsync(
        BootstrapRecoveryExpectation expectation,
        string claimToken,
        string finalJobId,
        bool isRecovery,
        CancellationToken ct);

    /// <summary>
    /// Transfers scalable parent-start job ownership to a scheduled replacement. The
    /// request job id, linked parent id, fencing token, and pending parent phase must all
    /// still match; a stale replacement must exit without side effects.
    /// </summary>
    Task<bool> TryReassignScalableStartJobAsync(
        Guid requestId,
        Guid parentId,
        Guid fencingToken,
        string expectedJobId,
        string? expectedPhaseJobId,
        string replacementJobId,
        CancellationToken ct);

    /// <summary>
    /// Atomically records a parent-start replacement scheduling failure on the
    /// linked request and pending parent. The third failure terminalizes both.
    /// </summary>
    Task<bool> TryRecordScalableStartSchedulingFailureAsync(
        BootstrapRecoveryExpectation requestExpectation,
        Guid parentId,
        Guid fencingToken,
        string expectedParentStatus,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct) =>
        throw new NotSupportedException("The store must implement atomic parent-start scheduling failure.");

    /// <summary>
    /// Atomically fails a claimed request. CAS on <paramref name="claimToken"/>
    /// matching <c>reconcile_claim_token</c>. Sets status = failed and clears the claim.
    /// Returns <c>true</c> when the CAS succeeded.
    /// </summary>
    Task<bool> TryFailClaimAsync(
        Guid requestId, string claimToken, string errorCode, string errorMessage,
        CancellationToken ct);

    /// <summary>
    /// Atomically marks a request as failed for a recovery-specific error (exhaustion
    /// or inconsistent state). Guards with CAS on <paramref name="expectation"/>.
    /// </summary>
    Task<bool> TryMarkRecoveryFailedAsync(
        BootstrapRecoveryExpectation expectation, string errorCode, string errorMessage,
        CancellationToken ct);

    /// <summary>
    /// Atomically fails a scalable request and its linked active parent. Every request
    /// and parent ownership predicate must match or neither durable row is changed.
    /// </summary>
    Task<bool> TryFailScalableRecoveryExhaustedAsync(BootstrapRecoveryExpectation expectation,
        Guid parentId, Guid fencingToken, string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt, string? expectedPhaseJobId,
        string errorCode, string errorMessage, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement atomic scheduling exhaustion.");

    /// <summary>
    /// Atomically claims request recovery ownership AND parent phase-job ownership for
    /// scalable CatchingUp/Publishing recovery. A single claim token is written to both
    /// durable rows in one transaction. Returns false if any CAS guard does not match.
    /// </summary>
    Task<bool> TryClaimScalableRecoveryAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string claimToken,
        bool isRecovery,
        CancellationToken ct);

    /// <summary>
    /// Atomically transfers a scalable recovery claim to its executing finalize job.
    /// This clears both claims and records request recovery accounting exactly once.
    /// </summary>
    Task<bool> TryFinalizeScalableRecoveryClaimAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string claimToken,
        string finalJobId,
        CancellationToken ct);

    /// <summary>
    /// Atomically fails a request and its linked parent after a child has failed.
    /// Both durable snapshots must match or neither row is modified.
    /// </summary>
    Task<bool> TryFailScalableChildRecoveryAsync(
        BootstrapRecoveryExpectation expectation,
        BootstrapChildFailureExpectation childExpectation,
        Guid parentId,
        Guid fencingToken,
        string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct);

    /// <summary>Atomically terminalizes a failed child, its fenced parent, and linked request.</summary>
    Task<bool> TryFailScalableChildAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapChildFailureExpectation childExpectation,
        Guid fencingToken,
        string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct);

    Task<bool> TryFailScalableChildSchedulingExhaustedAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapChildFailureExpectation childExpectation,
        Guid fencingToken, string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt, string? expectedPhaseJobId,
        string errorCode, string errorMessage, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement atomic scheduling exhaustion.");

    /// <summary>Atomically terminalizes a fenced parent and linked request.</summary>
    Task<bool> TryFailScalableAsync(
        BootstrapRecoveryExpectation requestExpectation,
        Guid parentId,
        Guid fencingToken,
        string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct);

    /// <summary>Atomically terminalizes a paired request/parent recovery claim after scheduling fails.</summary>
    Task<bool> TryFailScalableRecoveryClaimAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string claimToken,
        string errorCode,
        string errorMessage,
        CancellationToken ct);

    /// <summary>
    /// Atomically fails a scalable request and its linked active parent for an
    /// inconsistent phase state. Every request and parent ownership predicate must
    /// match or neither durable row is changed. Only fails when no fresh phase claim
    /// exists (claim_expired_at allowed to pass).
    /// </summary>
    Task<bool> TryFailInconsistentPhaseStateAsync(
        BootstrapRecoveryExpectation expectation,
        Guid parentId, Guid fencingToken,
        string expectedParentStatus, DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        DateTime claimExpiredAt,
        string errorCode, string errorMessage,
        CancellationToken ct);

    /// <summary>
    /// Returns active requests (running/waiting_for_lock) whose <c>updated_at</c>
    /// is at or before <paramref name="cutoffUtc"/> for stale-state inspection.
    /// </summary>
    Task<IReadOnlyList<BootstrapRequest>> GetStaleActiveBeforeAsync(
        string status, DateTime cutoffUtc, CancellationToken ct);
}
