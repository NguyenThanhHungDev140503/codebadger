namespace Application.Features.CentralDbSync.Services;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Microsoft.Extensions.Logging;

public sealed class BootstrapMonitorActionService : IBootstrapMonitorActionService
{
    private static readonly TimeSpan HeartbeatStaleThreshold = TimeSpan.FromMinutes(2);

    private static readonly HashSet<string> TerminalParentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        BootstrapParentStatus.Completed,
        BootstrapParentStatus.Failed,
        BootstrapParentStatus.Expired,
        BootstrapParentStatus.Cancelled
    };

    private static readonly HashSet<string> CancellableParentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        BootstrapParentStatus.Running,
        BootstrapParentStatus.CatchingUp,
        BootstrapParentStatus.Publishing
    };

    private static readonly HashSet<string> ReconcileableChildStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        BootstrapChildStatus.PendingEnqueue,
        BootstrapChildStatus.Queued,
        BootstrapChildStatus.Running
    };

    private readonly IBootstrapParentStore parentStore;
    private readonly IBootstrapChildStore childStore;
    private readonly IBootstrapJobScheduler jobScheduler;
    private readonly IBootstrapJobStateChecker jobChecker;
    private readonly IBootstrapDiagnosticEventStore eventStore;
    private readonly ILogger<BootstrapMonitorActionService> logger;

    public BootstrapMonitorActionService(
        IBootstrapParentStore parentStore,
        IBootstrapChildStore childStore,
        IBootstrapJobScheduler jobScheduler,
        IBootstrapJobStateChecker jobChecker,
        IBootstrapDiagnosticEventStore eventStore,
        ILogger<BootstrapMonitorActionService> logger)
    {
        this.parentStore = parentStore;
        this.childStore = childStore;
        this.jobScheduler = jobScheduler;
        this.jobChecker = jobChecker;
        this.eventStore = eventStore;
        this.logger = logger;
    }

    public async Task<BootstrapMonitorActionResult> ReconcileAsync(
        BootstrapMonitorTarget target, string initiatedBy, CancellationToken ct)
    {
        return target.IsChild
            ? await ReconcileChildAsync(target, initiatedBy, ct)
            : await ReconcileParentAsync(target, initiatedBy, ct);
    }

    public async Task<BootstrapMonitorActionResult> RetryAsync(
        BootstrapMonitorTarget target, string initiatedBy, CancellationToken ct)
    {
        if (!target.IsChild)
            return BootstrapMonitorActionResult.Conflict("Retry is only supported for child targets.");

        var parent = await parentStore.GetAsync(target.ParentId, ct);
        if (parent is null)
            return BootstrapMonitorActionResult.NotFound($"Parent {target.ParentId} not found.");

        var child = await childStore.GetAsync(target.ChildId!.Value, ct);
        if (child is null)
            return BootstrapMonitorActionResult.NotFound($"Child {target.ChildId} not found.");

        if (child.ParentId != target.ParentId)
            return BootstrapMonitorActionResult.NotFound(
                $"Child {child.ChildId} belongs to parent {child.ParentId}, not {target.ParentId}.");

        if (child.Status is BootstrapChildStatus.Running or BootstrapChildStatus.Completed)
            return BootstrapMonitorActionResult.Conflict(
                $"Child {child.ChildId} is in status '{child.Status}' and cannot be retried.");

        if (child.Status is not BootstrapChildStatus.Failed)
            return BootstrapMonitorActionResult.Conflict(
                $"Child {child.ChildId} is in status '{child.Status}' — only Failed children can be retried.");

        var claimToken = Guid.NewGuid().ToString("N");

        var result = await childStore.TryClaimRetryAsync(
            child.ChildId, child.ParentId,
            child.Status, child.HangfireJobId,
            claimToken, ct);

        if (!result.Claimed)
            return BootstrapMonitorActionResult.Conflict("Retry claim lost — another agent already claimed this child.");

        string jobId;
        try
        {
            jobId = await jobScheduler.ScheduleClaimedChildAsync(
                parent.RuleName, parent.ParentId, child.ChildId,
                parent.FencingToken, BootstrapChildStatus.PendingEnqueue, claimToken, ct);
        }
        catch (Exception ex)
        {
            var errorMessage = BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Unknown scheduler error";
            await AppendDiagnosticEventAsync(
                parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, child.ChildId,
                BootstrapDiagnosticEntityType.Child,
                BootstrapDiagnosticEventType.ScheduleFailure,
                child.Status, child.Status, null,
                parent.FencingToken.ToString(), child.Sequence, null,
                $"Retry schedule failed: {errorMessage}", initiatedBy, ct);
            return BootstrapMonitorActionResult.SchedulerFailure(errorMessage);
        }

        await AppendDiagnosticEventAsync(
            parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, child.ChildId,
            BootstrapDiagnosticEntityType.Child,
            BootstrapDiagnosticEventType.RetryRequested,
            child.Status, child.Status, jobId,
            parent.FencingToken.ToString(), child.Sequence, null,
            $"Manual retry requested by {initiatedBy}", initiatedBy, ct);

        return BootstrapMonitorActionResult.Accepted(jobId);
    }

    public async Task<BootstrapMonitorActionResult> RequestCancelAsync(
        Guid parentId, string initiatedBy, CancellationToken ct)
    {
        var parent = await parentStore.GetAsync(parentId, ct);
        if (parent is null)
            return BootstrapMonitorActionResult.NotFound($"Parent {parentId} not found.");

        if (TerminalParentStatuses.Contains(parent.Status))
            return BootstrapMonitorActionResult.Conflict(
                $"Parent {parentId} is in terminal status '{parent.Status}' and cannot be cancelled.");

        if (!CancellableParentStatuses.Contains(parent.Status))
            return BootstrapMonitorActionResult.Conflict(
                $"Parent {parentId} is in status '{parent.Status}' — only Running, CatchingUp, and Publishing parents can be cancelled.");

        if (!await parentStore.TryRequestCancelAsync(parentId, parent.FencingToken, initiatedBy, ct))
            return BootstrapMonitorActionResult.Conflict("Cancel CAS lost — another agent already handled this request.");

        await AppendDiagnosticEventAsync(
            parent.BootstrapRequestId ?? parent.ParentId, parentId, null,
            BootstrapDiagnosticEntityType.Parent,
            BootstrapDiagnosticEventType.CancelRequested,
            parent.Status, BootstrapParentStatus.CancelRequested, null,
            parent.FencingToken.ToString(), null, null,
            $"Cancel requested by {initiatedBy}", initiatedBy, ct);

        string cleanupJobId;
        try
        {
            cleanupJobId = await jobScheduler.EnqueueCancelCleanupAsync(parentId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Cancellation was recorded for parent {ParentId}, but immediate cleanup could not be scheduled",
                parentId);
            return BootstrapMonitorActionResult.SchedulerFailure(
                "Cancellation was recorded, but cleanup could not be scheduled. Daily recovery will retry it.");
        }

        return BootstrapMonitorActionResult.Accepted(cleanupJobId);
    }

    private async Task<BootstrapMonitorActionResult> ReconcileParentAsync(
        BootstrapMonitorTarget target, string initiatedBy, CancellationToken ct)
    {
        var parent = await parentStore.GetAsync(target.ParentId, ct);
        if (parent is null)
            return BootstrapMonitorActionResult.NotFound($"Parent {target.ParentId} not found.");

        if (TerminalParentStatuses.Contains(parent.Status))
            return BootstrapMonitorActionResult.Conflict(
                $"Parent {target.ParentId} is in terminal status '{parent.Status}' — reconcile not applicable.");

        if (!CancellableParentStatuses.Contains(parent.Status))
            return BootstrapMonitorActionResult.Conflict(
                $"Parent {target.ParentId} is in status '{parent.Status}' — reconcile not applicable.");

        var isStale = parent.LastHeartbeatAt is null
                      || parent.LastHeartbeatAt < DateTime.UtcNow - HeartbeatStaleThreshold;
        var missingPhase = parent.PhaseClaimToken is null;

        if (!isStale && !missingPhase)
            return BootstrapMonitorActionResult.Conflict(
                $"Parent {target.ParentId} heartbeat is recent and phase ownership is present — no reconcile needed.");

        var phaseJobObserved = jobChecker.Probe(parent.PhaseJobId);
        if (phaseJobObserved.Kind == BootstrapJobStateKind.Alive)
            return BootstrapMonitorActionResult.Conflict(
                $"Parent {target.ParentId} phase job {parent.PhaseJobId} is alive — no reconcile needed.");

        var claimToken = Guid.NewGuid().ToString("N");
        var staleBefore = DateTime.UtcNow - HeartbeatStaleThreshold;

        if (!await parentStore.TryClaimPhaseJobAsync(
                parent.ParentId, parent.FencingToken, parent.Status,
                parent.PhaseJobId, claimToken, staleBefore, ct))
            return BootstrapMonitorActionResult.Conflict("Phase claim CAS lost — another agent already claimed this parent.");

        var children = await childStore.GetByParentAsync(parent.ParentId, ct);
        var activeChild = children.LastOrDefault(c => c.Status is BootstrapChildStatus.PendingEnqueue
            or BootstrapChildStatus.Queued or BootstrapChildStatus.Running);
        if (activeChild is not null)
        {
            var childClaimToken = Guid.NewGuid().ToString("N");
            var childStaleBefore = DateTime.UtcNow - HeartbeatStaleThreshold;
            var childExpectation = new BootstrapChildRecoveryExpectation(
                activeChild.ChildId, parent.ParentId, parent.FencingToken,
                activeChild.Status, activeChild.HangfireJobId,
                childClaimToken, childStaleBefore);
            if (!await childStore.TryClaimRecoveryAsync(childExpectation, ct))
                return BootstrapMonitorActionResult.Conflict("Child recovery claim lost — another agent claimed this child.");

            string childJobId;
            try
            {
                childJobId = await jobScheduler.ScheduleClaimedChildAsync(
                    parent.RuleName, parent.ParentId, activeChild.ChildId,
                    parent.FencingToken, activeChild.Status, childClaimToken, ct);
            }
            catch (Exception ex)
            {
                var errorMessage = BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Unknown scheduler error";
                await AppendDiagnosticEventAsync(
                    parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, activeChild.ChildId,
                    BootstrapDiagnosticEntityType.Child,
                    BootstrapDiagnosticEventType.ScheduleFailure,
                    activeChild.Status, activeChild.Status, null,
                    parent.FencingToken.ToString(), activeChild.Sequence, null,
                    $"Child schedule failed: {errorMessage}", initiatedBy, ct);
                return BootstrapMonitorActionResult.SchedulerFailure(errorMessage);
            }

            await AppendDiagnosticEventAsync(
                parent.ParentId, parent.ParentId, activeChild.ChildId,
                BootstrapDiagnosticEntityType.Child,
                BootstrapDiagnosticEventType.ReconcileRequested,
                activeChild.Status, activeChild.Status, childJobId,
                parent.FencingToken.ToString(), activeChild.Sequence, null,
                $"Reconcile (via parent) requested by {initiatedBy}", initiatedBy, ct);

            return BootstrapMonitorActionResult.Accepted(childJobId);
        }

        var failedChild = children.LastOrDefault(c => c.Status == BootstrapChildStatus.Failed);
        if (failedChild is not null)
        {
            var retryClaimToken = Guid.NewGuid().ToString("N");
            var retryResult = await childStore.TryClaimRetryAsync(
                failedChild.ChildId, parent.ParentId,
                failedChild.Status, failedChild.HangfireJobId,
                retryClaimToken, ct);
            if (!retryResult.Claimed)
                return BootstrapMonitorActionResult.Conflict("Retry claim lost — another agent claimed this child.");

            string retryJobId;
            try
            {
                retryJobId = await jobScheduler.ScheduleClaimedChildAsync(
                    parent.RuleName, parent.ParentId, failedChild.ChildId,
                    parent.FencingToken, BootstrapChildStatus.PendingEnqueue, retryClaimToken, ct);
            }
            catch (Exception ex)
            {
                var errorMessage = BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Unknown scheduler error";
                await AppendDiagnosticEventAsync(
                    parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, failedChild.ChildId,
                    BootstrapDiagnosticEntityType.Child,
                    BootstrapDiagnosticEventType.ScheduleFailure,
                    failedChild.Status, BootstrapChildStatus.PendingEnqueue, null,
                    parent.FencingToken.ToString(), failedChild.Sequence, null,
                    $"Retry schedule failed: {errorMessage}", initiatedBy, ct);
                return BootstrapMonitorActionResult.SchedulerFailure(errorMessage);
            }

            await AppendDiagnosticEventAsync(
                parent.ParentId, parent.ParentId, failedChild.ChildId,
                BootstrapDiagnosticEntityType.Child,
                BootstrapDiagnosticEventType.RetryRequested,
                failedChild.Status, BootstrapChildStatus.PendingEnqueue, retryJobId,
                parent.FencingToken.ToString(), failedChild.Sequence, null,
                $"Retry (via parent reconcile) requested by {initiatedBy}", initiatedBy, ct);

            return BootstrapMonitorActionResult.Accepted(retryJobId);
        }

        string jobId;
        try
        {
            jobId = await jobScheduler.ScheduleClaimedFinalizeAsync(
                parent.RuleName, parent.ParentId, parent.FencingToken,
                parent.Status, claimToken, ct);
        }
        catch (Exception ex)
        {
            var errorMessage = BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Unknown scheduler error";
            await parentStore.TryRecordPhaseClaimSchedulingFailureAsync(
                parent.ParentId, parent.FencingToken, parent.Status,
                claimToken, "ScheduleFailure", errorMessage, ct);
            await AppendDiagnosticEventAsync(
                parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, null,
                BootstrapDiagnosticEntityType.Parent,
                BootstrapDiagnosticEventType.ScheduleFailure,
                parent.Status, parent.Status, null,
                parent.FencingToken.ToString(), null, null,
                $"Schedule failed: {errorMessage}", initiatedBy, ct);
            return BootstrapMonitorActionResult.SchedulerFailure(errorMessage);
        }

        await AppendDiagnosticEventAsync(
            parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, null,
            BootstrapDiagnosticEntityType.Parent,
            BootstrapDiagnosticEventType.ReconcileRequested,
            parent.Status, parent.Status, jobId,
            parent.FencingToken.ToString(), null, null,
            $"Reconcile requested by {initiatedBy}", initiatedBy, ct);

        return BootstrapMonitorActionResult.Accepted(jobId);
    }

    private async Task<BootstrapMonitorActionResult> ReconcileChildAsync(
        BootstrapMonitorTarget target, string initiatedBy, CancellationToken ct)
    {
        var parent = await parentStore.GetAsync(target.ParentId, ct);
        if (parent is null)
            return BootstrapMonitorActionResult.NotFound($"Parent {target.ParentId} not found.");

        var child = await childStore.GetAsync(target.ChildId!.Value, ct);
        if (child is null)
            return BootstrapMonitorActionResult.NotFound($"Child {target.ChildId} not found.");

        if (child.ParentId != target.ParentId)
            return BootstrapMonitorActionResult.NotFound(
                $"Child {child.ChildId} belongs to parent {child.ParentId}, not {target.ParentId}.");

        if (!ReconcileableChildStatuses.Contains(child.Status))
            return BootstrapMonitorActionResult.Conflict(
                $"Child {child.ChildId} is in status '{child.Status}' — reconcile not applicable.");

        var isStale = child.LastHeartbeatAt is null
                      || child.LastHeartbeatAt < DateTime.UtcNow - HeartbeatStaleThreshold;
        var missingHangfire = child.HangfireJobId is null;

        if (!isStale && !missingHangfire)
            return BootstrapMonitorActionResult.Conflict(
                $"Child {child.ChildId} heartbeat is recent and Hangfire job is present — no reconcile needed.");

        var claimToken = Guid.NewGuid().ToString("N");
        var staleBefore = DateTime.UtcNow - HeartbeatStaleThreshold;

        var expectation = new BootstrapChildRecoveryExpectation(
            child.ChildId,
            child.ParentId,
            parent.FencingToken,
            child.Status,
            child.HangfireJobId,
            claimToken,
            staleBefore);

        if (!await childStore.TryClaimRecoveryAsync(expectation, ct))
            return BootstrapMonitorActionResult.Conflict("Recovery claim lost — another agent recovered this child.");

        string jobId;
        try
        {
            jobId = await jobScheduler.ScheduleClaimedChildAsync(
                parent.RuleName, parent.ParentId, child.ChildId,
                parent.FencingToken, child.Status, claimToken, ct);
        }
        catch (Exception ex)
        {
            var errorMessage = BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Unknown scheduler error";
            await childStore.TryRecordRecoverySchedulingFailureAsync(expectation, "ScheduleFailure", errorMessage, ct);
            await AppendDiagnosticEventAsync(
                parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, child.ChildId,
                BootstrapDiagnosticEntityType.Child,
                BootstrapDiagnosticEventType.ScheduleFailure,
                child.Status, child.Status, null,
                parent.FencingToken.ToString(), child.Sequence, null,
                $"Schedule failed: {errorMessage}", initiatedBy, ct);
            return BootstrapMonitorActionResult.SchedulerFailure(errorMessage);
        }

        await AppendDiagnosticEventAsync(
            parent.BootstrapRequestId ?? parent.ParentId, parent.ParentId, child.ChildId,
            BootstrapDiagnosticEntityType.Child,
            BootstrapDiagnosticEventType.ReconcileRequested,
            child.Status, child.Status, jobId,
            parent.FencingToken.ToString(), child.Sequence, null,
            $"Reconcile requested by {initiatedBy}", initiatedBy, ct);

        return BootstrapMonitorActionResult.Accepted(jobId);
    }

    private async Task AppendDiagnosticEventAsync(
        Guid requestId, Guid parentId, Guid? childId,
        string entityType, string eventType,
        string? fromStatus, string? toStatus, string? hangfireJobId,
        string? fencingToken, int? childSequence, long? rowsAffected,
        string? message, string initiatedBy, CancellationToken ct)
    {
        var evt = BootstrapDiagnosticEvent.Create(
            requestId, parentId, childId,
            entityType, eventType,
            fromStatus, toStatus,
            hangfireJobId, fencingToken,
            childSequence, rowsAffected,
            eventType, message,
            initiatedBy);
        await eventStore.AppendAsync(evt, ct);
    }
}
