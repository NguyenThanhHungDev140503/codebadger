using Application.Features.CentralDbSync.Models;
using System.Data.Common;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Store contract for the CT dispatch outbox marker.
/// One marker per successful final publish ensures exactly one CT continuation
/// runs from C1. Idempotent — duplicate continuation jobs are harmless because
/// CT checkpoint advancement uses CAS.
/// </summary>
public interface IBootstrapCtDispatchStore
{
    /// <summary>
    /// Creates a dispatch marker with status 'pending_dispatch' inside a transaction.
    /// Called inside the final publish transaction. Returns the dispatch ID.
    /// </summary>
    Task<Guid> CreateInTransactionAsync(DbConnection connection, DbTransaction transaction,
        string ruleName, Guid parentId, long watermark, CancellationToken ct);

    /// <summary>
    /// Returns dispatch candidates: pending_dispatch markers or dispatching markers
    /// whose lease has expired (dispatch_lease_until &lt; nowUtc).
    /// </summary>
    Task<IReadOnlyList<BootstrapCtDispatch>> GetDispatchCandidatesAsync(
        DateTime nowUtc, CancellationToken ct);

    /// <summary>
    /// Atomic CAS claim: transitions a candidate to 'dispatching' with a lease
    /// window and a new lease token. Only rows where status is 'pending_dispatch',
    /// or status is 'dispatching' AND dispatch_lease_until &lt; nowUtc, are eligible.
    /// Increments attempt_count, sets dispatch_lease_until = leaseUntilUtc,
    /// and generates a new dispatch_lease_token.
    /// Returns the lease token if claimed, or null if another worker claimed it first.
    /// </summary>
    Task<Guid?> TryClaimForDispatchAsync(Guid dispatchId, DateTime leaseUntilUtc,
        CancellationToken ct);

    /// <summary>
    /// Marks a dispatch as successfully dispatched after the Hangfire job was
    /// enqueued. Sets status = 'dispatched', hangfire_job_id, dispatched_at = NOW(),
    /// and clears dispatch_lease_until.
    /// Guards by dispatch_lease_token — a stale worker with an expired lease cannot
    /// modify a marker it no longer owns.
    /// </summary>
    Task MarkDispatchedAsync(Guid dispatchId, Guid dispatchLeaseToken,
        string hangfireJobId, CancellationToken ct);

    /// <summary>
    /// Releases a claimed dispatch back to pending_dispatch for retry after a
    /// failed enqueue. Clears dispatch_lease_until and records last_error.
    /// Guards by dispatch_lease_token — a stale worker with an expired lease cannot
    /// release a marker it no longer owns.
    /// </summary>
    Task ReleaseForRetryAsync(Guid dispatchId, Guid dispatchLeaseToken,
        string safeError, CancellationToken ct);
}
