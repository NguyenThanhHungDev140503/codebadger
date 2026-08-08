namespace Application.Features.CentralDbSync.Services;

using Application.Features.CentralDbSync.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Dispatches pending CT continuation jobs from the
/// <c>bootstrap_ct_dispatch</c> outbox. Uses a lease-based CAS claim to
/// guarantee at-least-once dispatch across competing workers.
/// </summary>
public sealed class BootstrapCtDispatchService(
    IBootstrapCtDispatchStore dispatchStore,
    IBootstrapJobScheduler jobScheduler,
    ILogger<BootstrapCtDispatchService> logger) : IBootstrapCtDispatchService
{
    /// <summary>
    /// Lease duration for a claimed dispatch marker. If the worker crashes
    /// before marking the dispatch as complete, the lease expires and another
    /// worker can retry it.
    /// </summary>
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public async Task DispatchPendingAsync(CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var candidates = await dispatchStore.GetDispatchCandidatesAsync(nowUtc, ct);

        foreach (var candidate in candidates)
        {
            var leaseUntil = nowUtc.Add(LeaseDuration);
            var leaseToken = await dispatchStore.TryClaimForDispatchAsync(
                candidate.DispatchId, leaseUntil, ct);

            if (leaseToken is null)
            {
                logger.LogDebug(
                    "CT dispatch {DispatchId} for rule {RuleName} could not be claimed — another worker took it",
                    candidate.DispatchId, candidate.RuleName);
                continue;
            }

            try
            {
                logger.LogInformation(
                    "Claimed CT dispatch {DispatchId} for rule {RuleName} (parent {ParentId}, WM {Watermark})",
                    candidate.DispatchId, candidate.RuleName, candidate.ParentId, candidate.Watermark);

                var hangfireJobId = await jobScheduler.EnqueueCtContinuationAsync(
                    candidate.RuleName, candidate.ParentId, ct);

                await dispatchStore.MarkDispatchedAsync(
                    candidate.DispatchId, leaseToken.Value, hangfireJobId, ct);

                logger.LogInformation(
                    "Dispatched CT continuation job {JobId} for rule {RuleName} (dispatch {DispatchId})",
                    hangfireJobId, candidate.RuleName, candidate.DispatchId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to dispatch CT continuation for {DispatchId} (rule {RuleName}), releasing for retry",
                    candidate.DispatchId, candidate.RuleName);

                var safeError = $"{ex.GetType().Name}: {ex.Message}";
                await dispatchStore.ReleaseForRetryAsync(
                    candidate.DispatchId, leaseToken.Value, safeError, ct);
            }
        }
    }
}
