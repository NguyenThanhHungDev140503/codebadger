namespace Infrastructure.CentralDbSync;

using Application.Common.Exceptions;
using Application.Features.CentralDbSync.Abstractions;
using Domain.Enums;
using Hangfire;

public sealed class HangfireBootstrapJobScheduler(IBackgroundJobClient client)
    : IBootstrapJobScheduler
{
    public Task<string> EnqueueAsync(string sourceTable, Guid requestId, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunBootstrapAsync(sourceTable, requestId))));
    }

    public Task<string> ScheduleAsync(string sourceTable, Guid requestId, TimeSpan delay, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Schedule<CentralDbSyncJobs>(
            job => job.RunBootstrapAsync(sourceTable, requestId), delay)));
    }

    public Task ScheduleWatchdogAsync(string sourceTable, Guid requestId, TimeSpan delay, CancellationToken ct)
    {
        Guard(() => client.Schedule<CentralDbSyncJobs>(
            job => job.ReconcileBootstrapRequestAsync(sourceTable, requestId),
            delay));
        return Task.CompletedTask;
    }

    public Task<string> EnqueueParentStartAsync(string ruleName, Guid parentId, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunParentStartAsync(null!, ruleName, parentId))));
    }

    public Task<string> EnqueueChildAsync(string ruleName, Guid parentId, Guid childId, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunChildBootstrapAsync(null!, ruleName, parentId, childId))));
    }

    public Task<string> ScheduleClaimedChildAsync(string ruleName, Guid parentId, Guid childId,
        Guid fencingToken, string expectedStatus, string claimToken, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunClaimedChildBootstrapAsync(null!, ruleName, parentId, childId,
                fencingToken, expectedStatus, claimToken))));
    }

    public Task<string> EnqueueFinalizeAsync(string ruleName, Guid parentId, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunCoordinatorFinalizeAsync(null!, ruleName, parentId))));
    }

    public Task<string> EnqueueCancelCleanupAsync(Guid parentId, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunCancelBootstrapCleanupAsync(parentId))));
    }

    public Task<string> ScheduleClaimedFinalizeAsync(string ruleName, Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunClaimedCoordinatorFinalizeAsync(null!, ruleName, parentId, fencingToken,
                expectedStatus, claimToken))));
    }

    public Task<string> ScheduleClaimedScalableRecoveryFinalizeAsync(string ruleName, Guid requestId,
        Guid parentId, Guid fencingToken, string expectedStatus, string? expectedPhaseJobId,
        string claimToken, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunClaimedScalableRecoveryCoordinatorFinalizeAsync(null!, ruleName, requestId,
                parentId, fencingToken, expectedStatus, expectedPhaseJobId, claimToken))));
    }

    public Task<string> EnqueueCtContinuationAsync(string ruleName, Guid parentId, CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Enqueue<CentralDbSyncJobs>(
            job => job.RunBootstrapCtContinuationAsync(ruleName, parentId))));
    }

    public Task<string> ScheduleClaimedAsync(
        string sourceTable,
        Guid requestId,
        string claimToken,
        bool isRecovery,
        TimeSpan delay,
        CancellationToken ct)
    {
        return Task.FromResult(Guard(() => client.Schedule<CentralDbSyncJobs>(
            job => job.RunClaimedBootstrapAsync(
                null!, sourceTable, requestId, claimToken, isRecovery),
            delay)));
    }

    // Hangfire throws when its storage is unreachable. Surface that as an
    // unavailable feature so the API answers 503 instead of 500.
    private static string Guard(Func<string> enqueue)
    {
        try
        {
            return enqueue();
        }
        catch (Exception ex) when (ex is not FeatureUnavailableException)
        {
            throw new FeatureUnavailableException(
                Feature.CentralDbSync, FeatureUnavailableReason.ConnectionFailed);
        }
    }
}
