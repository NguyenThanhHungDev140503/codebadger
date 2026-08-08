namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Dispatches pending <c>bootstrap_ct_dispatch</c> outbox markers by claiming
/// them with a lease, enqueuing the CT continuation Hangfire job, and marking
/// them as dispatched. Designed for use by a recurring reconciliation job.
/// </summary>
public interface IBootstrapCtDispatchService
{
    /// <summary>
    /// Processes all dispatch candidates that are pending or have expired leases.
    /// For each candidate, tries to claim it with a lease, enqueues the CT
    /// continuation job, and marks it dispatched (or releases for retry on failure).
    /// </summary>
    Task DispatchPendingAsync(CancellationToken ct);
}
