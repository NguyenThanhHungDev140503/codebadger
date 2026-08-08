namespace Application.Features.CentralDbSync.Abstractions;

public interface IBootstrapJobScheduler
{
    Task<string> EnqueueAsync(string sourceTable, Guid requestId, CancellationToken ct);
    Task<string> ScheduleAsync(string sourceTable, Guid requestId, TimeSpan delay, CancellationToken ct);

    /// <summary>
    /// Schedules a one-shot watchdog that re-enqueues the bootstrap job only if the
    /// request is still stuck in pending_enqueue after <paramref name="delay"/>.
    /// </summary>
    Task ScheduleWatchdogAsync(string sourceTable, Guid requestId, TimeSpan delay, CancellationToken ct);

    /// <summary>
    /// Enqueues the scalable coordinator start job for a parent.
    /// </summary>
    Task<string> EnqueueParentStartAsync(string ruleName, Guid parentId, CancellationToken ct);

    /// <summary>
    /// Enqueues a child bootstrap job on the dedicated bootstrap-child queue.
    /// </summary>
    Task<string> EnqueueChildAsync(string ruleName, Guid parentId, Guid childId, CancellationToken ct);

    /// <summary>Schedules a child that must finalize its durable recovery claim first.</summary>
    Task<string> ScheduleClaimedChildAsync(string ruleName, Guid parentId, Guid childId,
        Guid fencingToken, string expectedStatus, string claimToken, CancellationToken ct);

    /// <summary>
    /// Enqueues the coordinator finalize job (CT catch-up + publish) after EOF.
    /// </summary>
    Task<string> EnqueueFinalizeAsync(string ruleName, Guid parentId, CancellationToken ct);

    /// <summary>Enqueues immediate cleanup for a parent in cancel_requested state.</summary>
    Task<string> EnqueueCancelCleanupAsync(Guid parentId, CancellationToken ct);

    /// <summary>Schedules a finalize job that must finalize durable parent phase ownership first.</summary>
    Task<string> ScheduleClaimedFinalizeAsync(string ruleName, Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken, CancellationToken ct);

    /// <summary>Schedules a finalize job that atomically finalizes linked request and parent recovery claims.</summary>
    Task<string> ScheduleClaimedScalableRecoveryFinalizeAsync(string ruleName, Guid requestId,
        Guid parentId, Guid fencingToken, string expectedStatus, string? expectedPhaseJobId,
        string claimToken, CancellationToken ct);

    /// <summary>
    /// Enqueues the CT continuation job for scalable bootstrap after the final
    /// publish transaction commits at C1. This runs the server-side CT catch-up
    /// from the watermark stored in the dispatch marker.
    /// </summary>
    Task<string> EnqueueCtContinuationAsync(string ruleName, Guid parentId, CancellationToken ct);

    /// <summary>
    /// Schedules a fenced bootstrap job that carries the ownership claim. The job
    /// self-finalizes the claim with its actual Hangfire id before side effects.
    /// </summary>
    Task<string> ScheduleClaimedAsync(
        string sourceTable,
        Guid requestId,
        string claimToken,
        bool isRecovery,
        TimeSpan delay,
        CancellationToken ct);
}
