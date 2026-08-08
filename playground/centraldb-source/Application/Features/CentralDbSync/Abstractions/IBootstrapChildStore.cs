using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Result of an atomic retry claim operation.
/// </summary>
public sealed record BootstrapChildRetryResult
{
    public bool Claimed { get; init; }
    public BootstrapChild? Child { get; init; }
    public BootstrapParent? Parent { get; init; }
}

/// <summary>
/// CAS-oriented store for bootstrap_child lifecycle.
/// Children are created lazily by the parent coordinator.
/// Only the next uncompleted child can be claimed for a given parent.
/// </summary>
public interface IBootstrapChildStore
{
    /// <summary>
    /// Creates the next sequential child for a parent in PendingEnqueue status.
    /// afterKey is the last primary key from the previous child (null for child 1).
    /// </summary>
    Task<BootstrapChild> CreateNextAsync(Guid parentId, string? afterKey,
        CancellationToken ct);

    /// <summary>
    /// Idempotent next-child creation with CAS guard. Verifies parent fencing token
    /// and that the latest completed child matches expected snapshot. If the next
    /// child already exists (duplicate run), returns the existing one instead of
    /// throwing. Only the winner creates and may schedule.
    /// </summary>
    Task<BootstrapNextChildResult> TryCreateNextChildAsync(
        Guid parentId, Guid fencingToken,
        int expectedLatestCompletedSequence,
        string? expectedLatestCompletedLastKey,
        string? afterKey,
        CancellationToken ct);

    /// <summary>Returns child by ID, or null if not found.</summary>
    Task<BootstrapChild?> GetAsync(Guid childId, CancellationToken ct);

    /// <summary>Returns all children for a parent, ordered by sequence.</summary>
    Task<IReadOnlyList<BootstrapChild>> GetByParentAsync(Guid parentId,
        CancellationToken ct);

    /// <summary>
    /// CAS-claims a child as Running. Only succeeds if the child is the next
    /// uncompleted child (sequence check) and status is PendingEnqueue.
    /// </summary>
    Task<bool> TryClaimAsync(Guid childId, Guid parentId, CancellationToken ct);

    Task<bool> TryClaimInitialAsync(Guid childId, Guid parentId, Guid fencingToken,
        string claimToken, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement fenced child claims.");

    Task<bool> TryClaimAsync(Guid childId, Guid parentId, Guid fencingToken,
        CancellationToken ct) =>
        throw new NotSupportedException("The store must implement fenced child claims.");

    Task<bool> TryFinalizeInitialClaimAsync(Guid childId, Guid parentId, Guid fencingToken,
        string claimToken, string actualJobId, CancellationToken ct) =>
        throw new NotSupportedException("The store must implement fenced claim finalization.");

    /// <summary>
    /// Marks a child as Completed with its last primary key and row count.
    /// Returns false if the child was already completed or parent token mismatch.
    /// </summary>
    [Obsolete("Use fenced TryCompleteAsync with the parent fencing token.")]
    Task<bool> CompleteAsync(Guid childId, Guid parentId, string? lastKey,
        long rowsRead, CancellationToken ct);
    Task<bool> TryCompleteAsync(Guid childId, Guid parentId, Guid fencingToken,
        string? lastKey, long rowsRead, CancellationToken ct);

    /// <summary>Marks a child as Failed with error details.</summary>
    Task<bool> MarkFailedAsync(Guid childId, Guid parentId, string errorCode,
        string errorMessage, CancellationToken ct);

    /// <summary>Updates last_heartbeat_at for the child.</summary>
    Task<bool> HeartbeatAsync(Guid childId, Guid parentId, CancellationToken ct);

    /// <summary>
    /// Sets the Hangfire job ID on a child. Used during enqueue reconciliation.
    /// </summary>
    [Obsolete("Use claim finalization with the actual Hangfire JobId.")]
    Task<bool> SetHangfireJobIdAsync(Guid childId, Guid parentId,
        string hangfireJobId, CancellationToken ct);

    /// <summary>
    /// Claims child recovery ownership before scheduling. The claim guards the durable
    /// child snapshot and parent fencing token; only its holder may enqueue a candidate.
    /// </summary>
    Task<bool> TryClaimRecoveryAsync(BootstrapChildRecoveryExpectation expectation,
        CancellationToken ct);

    /// <summary>Persists the actual Hangfire job id for a matching child claim.</summary>
    Task<bool> TryFinalizeRecoveryAsync(Guid childId, Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken, string actualJobId, CancellationToken ct);

    /// <summary>Persists scheduling evidence without releasing an owned recovery claim.</summary>
    Task<bool> TryRecordRecoverySchedulingFailureAsync(
        BootstrapChildRecoveryExpectation expectation,
        string errorCode,
        string errorMessage,
        CancellationToken ct);

    /// <summary>
    /// Atomically claims a failed child for retry without requiring the parent to be running.
    /// Transitions child from Failed → PendingEnqueue, clears error fields,
    /// resets rows_read, increments attempt_count, and sets reconcile_claim_token.
    /// Returns the child and its parent in a single atomic operation.
    /// </summary>
    Task<BootstrapChildRetryResult> TryClaimRetryAsync(
        Guid childId, Guid parentId,
        string expectedChildStatus,
        string? expectedChildHangfireJobId,
        string claimToken,
        CancellationToken ct);
}
