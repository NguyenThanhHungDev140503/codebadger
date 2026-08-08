using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Microsoft.Extensions.Logging;

namespace Application.Features.CentralDbSync.Services;

/// <summary>
/// Coordinates the full parent lifecycle of a scalable bootstrap:
/// startup (C0 capture + dynamic staging DDL + child 1),
/// finalization (CT catch-up + atomic publish + CT outbox dispatch).
/// Each phase runs as a separate Hangfire job.
/// </summary>
public sealed class ScalableBootstrapCoordinator(
    IBootstrapParentStore parentStore,
    IBootstrapDiagnosticEventStore eventStore,
    IBootstrapChildStore childStore,
    IStagedBootstrapSourceReader sourceReader,
    ITypedBootstrapStagingStore stagingStore,
    IBootstrapJobScheduler jobScheduler,
    IBootstrapCtCatchUpService ctCatchUpService,
    IBootstrapFinalPublisher finalPublisher,
    IMappingRuleProvider ruleProvider,
    IBootstrapRequestStore requestStore,
    BootstrapFailureService failureService,
    ILogger<ScalableBootstrapCoordinator> logger)
{
    private readonly IBootstrapDiagnosticEventStore _eventStore = eventStore;
    /// <summary>
    /// Captures C0, creates the dynamic staging table, creates child 1, and enqueues it.
    /// Designed to run as a Hangfire job on the data-sync queue.
    /// </summary>
    public async Task StartAsync(Guid parentId, CancellationToken ct)
    {
        var parent = await parentStore.GetAsync(parentId, ct);
        if (parent is null)
        {
            logger.LogError("Parent {ParentId} not found for start", parentId);
            return;
        }

        if (parent.Status == BootstrapParentStatus.CancelRequested)
        {
            await _eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                parent.BootstrapRequestId ?? Guid.Empty, parentId, null,
                BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.CancellationObserved,
                parent.Status, parent.Status, null, null, null, null,
                "CancellationObserved", "StartAsync aborted: parent is cancel_requested", "system"), ct);
            return;
        }

        // Claim parent (PendingEnqueue → Running)
        var claimed = await parentStore.TryClaimAsync(parentId, parent.FencingToken, ct);
        if (!claimed)
        {
            logger.LogWarning("Parent {ParentId} could not be claimed (already running or stale)", parentId);
            return;
        }

        // Move the shared ownership record out of Queued. Without this the request stays
        // Queued for the whole parent lifetime, and the one-shot watchdog treats the
        // finished start job as an orphan and enqueues a duplicate parent-start job.
        var rule = ruleProvider.Get(parent.RuleName);
        if (rule is null)
        {
            await FailParentAsync(parent, "RuleNotFound", $"Rule {parent.RuleName} not found", ct);
            return;
        }

        try
        {
            // Capture C0
            var preflight = await sourceReader.ValidateAndCaptureBaselineAsync(rule, ct);
            if (!preflight.IsValid)
            {
                await FailParentAsync(parent, preflight.ErrorCode!, preflight.ErrorMessage!, ct);
                return;
            }

            if (!await parentStore.SetBaselineVersionAsync(parentId, parent.FencingToken,
                    preflight.BaselineVersion, ct))
                return;

            {
                var current = await parentStore.GetAsync(parentId, ct);
                if (current?.Status == BootstrapParentStatus.CancelRequested)
                {
                    await _eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                        parent.BootstrapRequestId ?? Guid.Empty, parentId, null,
                        BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.CancellationObserved,
                        parent.Status, BootstrapParentStatus.CancelRequested, null, null, null, null,
                        "CancellationObserved", "StartAsync aborted after C0: parent is cancel_requested", "system"), ct);
                    return;
                }
            }

            // Use the authoritative staging table name from the parent record
            var stagingTableName = parent.StagingTableName;
            const string stagingSchema = "sync_meta";

            // Create dynamic staging table
            await stagingStore.CreateStageAsync(parentId, stagingTableName, rule, ct);
            if (!await parentStore.SetStagingCreatedAsync(parentId, parent.FencingToken, ct))
                return;

            {
                var current = await parentStore.GetAsync(parentId, ct);
                if (current?.Status == BootstrapParentStatus.CancelRequested)
                {
                    await _eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                        parent.BootstrapRequestId ?? Guid.Empty, parentId, null,
                        BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.CancellationObserved,
                        parent.Status, BootstrapParentStatus.CancelRequested, null, null, null, null,
                        "CancellationObserved", "StartAsync aborted after staging creation: parent is cancel_requested", "system"), ct);
                    return;
                }
            }

            // Create and enqueue child 1
            var child = await childStore.CreateNextAsync(parentId, null, ct);
            var childClaimToken = Guid.NewGuid().ToString("N");
            if (!await childStore.TryClaimInitialAsync(child.ChildId, parentId,
                    parent.FencingToken, childClaimToken, ct))
                return;
            await jobScheduler.ScheduleClaimedChildAsync(parent.RuleName, parentId,
                child.ChildId, parent.FencingToken, BootstrapChildStatus.PendingEnqueue,
                childClaimToken, ct);

            logger.LogInformation(
                "Parent {ParentId} for rule {RuleName} started: " +
                "C0={C0}, staging={Stage}, child1={ChildId}",
                parentId, parent.RuleName,
                preflight.BaselineVersion,
                $"{stagingSchema}.{stagingTableName}",
                child.ChildId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Parent {ParentId} start failed", parentId);
            await FailParentAsync(parent, "StartFailed",
                BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Bootstrap start failed.", ct);
        }
    }

    /// <summary>
    /// Captures C1, runs CT catch-up, then atomic final publish + DROP TABLE.
    /// If publish fails, the stage table survives for retry.
    /// On success, enqueues one CT continuation dispatch.
    /// </summary>
    public async Task FinalizeAsync(Guid parentId, CancellationToken ct)
    {
        var parent = await parentStore.GetAsync(parentId, ct);
        if (parent is null)
            return;

        if (parent.Status == BootstrapParentStatus.CancelRequested)
        {
            await _eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                parent.BootstrapRequestId ?? Guid.Empty, parentId, null,
                BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.CancellationObserved,
                parent.Status, parent.Status, null, null, null, null,
                "CancellationObserved", "FinalizeAsync aborted: parent is cancel_requested", "system"), ct);
            return;
        }

        switch (BootstrapParentRecoveryClassifier.Classify(parent))
        {
            case BootstrapParentRecoveryAction.ResumeRunning:
                await BeginCatchUpAsync(parent, ct);
                return;
            case BootstrapParentRecoveryAction.ResumeCatchingUp:
                await ResumeCatchingUpAsync(parent, ct);
                return;
            case BootstrapParentRecoveryAction.ResumePublishing:
                await ResumePublishingAsync(parent, ct);
                return;
            case BootstrapParentRecoveryAction.SyncCompleted:
                if (parent.BootstrapRequestId.HasValue)
                {
                    var request = await requestStore.GetAsync(parent.BootstrapRequestId.Value, ct);
                    if (request is not null)
                        await requestStore.TryCompleteAsync(request.RequestId, request.Status,
                            request.HangfireJobId ?? string.Empty, ct);
                }
                return;
            case BootstrapParentRecoveryAction.SyncFailed:
                if (parent.BootstrapRequestId.HasValue)
                {
                    var request = await requestStore.GetAsync(parent.BootstrapRequestId.Value, ct);
                    if (request is not null)
                        await requestStore.TryFailAsync(request.RequestId, request.Status,
                            request.HangfireJobId ?? string.Empty,
                            parent.ErrorCode ?? "ScalableBootstrapParentFailed",
                            parent.ErrorMessage ?? "Parent failed.", ct);
                }
                return;
            default:
                return;
        }
    }

    private async Task BeginCatchUpAsync(BootstrapParent parent, CancellationToken ct)
    {
        {
            var current = await parentStore.GetAsync(parent.ParentId, ct);
            if (current?.Status == BootstrapParentStatus.CancelRequested)
            {
                await _eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                    parent.BootstrapRequestId ?? Guid.Empty, parent.ParentId, null,
                    BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.CancellationObserved,
                    parent.Status, BootstrapParentStatus.CancelRequested, null, null, null, null,
                    "CancellationObserved", "BeginCatchUpAsync aborted: parent is cancel_requested", "system"), ct);
                return;
            }
        }

        var watermark = await sourceReader.GetCurrentVersionAsync(ct);
        if (!await parentStore.MarkCtCatchUpAsync(parent.ParentId, parent.FencingToken, watermark, ct))
            return;
        var updated = await parentStore.GetAsync(parent.ParentId, ct);
        if (updated is null) return;
        await ResumeCatchingUpAsync(updated, ct);
    }

    private async Task ResumeCatchingUpAsync(BootstrapParent parent, CancellationToken ct)
    {
        if (parent.BaselineVersion is null || parent.WatermarkVersion is null)
        {
            await FailParentAsync(parent, "NoDurableWatermark", "Cannot resume CT catch-up without durable baseline and watermark.", ct);
            return;
        }
        var rule = ruleProvider.Get(parent.RuleName);
        if (rule is null)
        {
            await FailParentAsync(parent, "RuleNotFound", $"Rule {parent.RuleName} not found", ct);
            return;
        }
        try
        {
            var catchUp = await ctCatchUpService.CatchUpAsync(rule, parent.BaselineVersion.Value,
                parent.WatermarkVersion.Value, parent.StagingSchema, parent.StagingTableName, ct);
            if (!catchUp.IsValid)
            {
                await FailParentAsync(parent, catchUp.ErrorCode!, catchUp.ErrorMessage!, ct);
                return;
            }
            if (!await parentStore.TryTransitionAsync(parent.ParentId, parent.FencingToken,
                    BootstrapParentStatus.CatchingUp, BootstrapParentStatus.Publishing, ct))
                return;
            var updated = await parentStore.GetAsync(parent.ParentId, ct);
            if (updated is null) return;
            await ResumePublishingAsync(updated, ct);
        }
        catch (Exception ex)
        {
            await FailParentAsync(parent, "FinalizeFailed",
                BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Bootstrap finalization failed.", ct);
        }
    }

    private async Task ResumePublishingAsync(BootstrapParent parent, CancellationToken ct)
    {
        if (parent.BaselineVersion is null || parent.WatermarkVersion is null)
        {
            await FailParentAsync(parent, "NoDurableWatermark", "Cannot publish without durable baseline and watermark.", ct);
            return;
        }
        var rule = ruleProvider.Get(parent.RuleName);
        if (rule is null)
        {
            await FailParentAsync(parent, "RuleNotFound", $"Rule {parent.RuleName} not found", ct);
            return;
        }
        try
        {
            {
                var current = await parentStore.GetAsync(parent.ParentId, ct);
                if (current?.Status == BootstrapParentStatus.CancelRequested)
                {
                    await _eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                        parent.BootstrapRequestId ?? Guid.Empty, parent.ParentId, null,
                        BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.CancellationObserved,
                        parent.Status, BootstrapParentStatus.CancelRequested, null, null, null, null,
                        "CancellationObserved", "ResumePublishingAsync aborted: parent is cancel_requested", "system"), ct);
                    return;
                }
            }

            var result = await finalPublisher.PublishAsync(rule, parent.ParentId, parent.FencingToken,
                parent.StagingSchema, parent.StagingTableName, parent.BaselineVersion.Value,
                parent.WatermarkVersion.Value, parent.BootstrapRequestId, ct);
            if (!result.IsSuccess)
                await FailParentAsync(parent, result.ErrorCode!, result.ErrorMessage!, ct);
        }
        catch (Exception ex)
        {
            await FailParentAsync(parent, "FinalizeFailed",
                BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Bootstrap finalization failed.", ct);
        }
    }

    private async Task FailParentAsync(
        BootstrapParent parent, string errorCode, string errorMessage, CancellationToken ct)
    {
        var fresh = await parentStore.GetAsync(parent.ParentId, ct);
        if (fresh is null) return;
        await failureService.FailAsync(fresh, null, errorCode, errorMessage, ct);
    }
}
