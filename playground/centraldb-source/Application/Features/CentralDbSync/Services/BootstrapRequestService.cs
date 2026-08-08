using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Application.Features.CentralDbSync.Validation;
using Microsoft.Extensions.Logging;

namespace Application.Features.CentralDbSync.Services;

public sealed class BootstrapRequestService
{
    private static readonly int MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength;

    private readonly IBootstrapRequestStore requestStore;
    private readonly IBootstrapJobScheduler scheduler;
    private readonly IMappingRuleProvider ruleProvider;
    private readonly IBootstrapJobStateChecker jobStateChecker;
    private readonly IBootstrapParentStore parentStore;
    private readonly IBootstrapChildStore childStore;
    private readonly IBootstrapReconciliationPolicy policy;
    private readonly ILogger<BootstrapRequestService> logger;

    public BootstrapRequestService(
        IBootstrapRequestStore requestStore,
        IBootstrapJobScheduler scheduler,
        IMappingRuleProvider ruleProvider,
        IBootstrapJobStateChecker jobStateChecker,
        IBootstrapParentStore parentStore,
        IBootstrapChildStore childStore,
        IBootstrapReconciliationPolicy policy,
        ILogger<BootstrapRequestService> logger)
    {
        this.requestStore = requestStore;
        this.scheduler = scheduler;
        this.ruleProvider = ruleProvider;
        this.jobStateChecker = jobStateChecker;
        this.parentStore = parentStore;
        this.childStore = childStore;
        this.policy = policy;
        this.logger = logger;
    }
    /// <summary>
    /// Delay before the one-shot orphan watchdog runs. Long enough for Hangfire to claim
    /// the primary job on a healthy path, short enough to recover crash orphans quickly.
    /// </summary>
    public static readonly TimeSpan WatchdogDelay = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Submits a bootstrap request for the given source table.
    /// Returns an existing active request if one exists, or creates a new one.
    /// Routes to the current in-memory flow or scalable parent-child flow based on
    /// <see cref="TableMappingRule.UseScalableBootstrap"/>.
    /// </summary>
    public async Task<BootstrapRequestResult> SubmitAsync(
        string ruleName, CancellationToken ct)
    {
        // Application-layer referential guard: rule must be registered.
        SyncGuard.AssertRegisteredRule(ruleName, ruleProvider, nameof(ruleName));

        var rule = ruleProvider.Get(ruleName);

        // Determine the bootstrap type before claiming shared ownership
        var bootstrapType = rule.UseScalableBootstrap
            ? BootstrapRequestType.Scalable
            : BootstrapRequestType.InMemory;

        // Shared ownership: claim a bootstrap_request for this rule_name.
        // Both branches (in_memory and scalable) share the same mechanism,
        // preventing concurrent bootstrap of either type for the same rule.
        var result = await requestStore.CreateOrGetActiveAsync(ruleName, ct, bootstrapType);

        if (!result.IsNewRequest)
        {
            logger.LogDebug(
                "Active bootstrap request {RequestId} already exists for {RuleName}",
                result.Request.RequestId, ruleName);
            return result;
        }

        var requestId = result.Request.RequestId;

        if (!rule.UseScalableBootstrap)
        {
            return await SubmitInMemoryAsync(ruleName, requestId, ct);
        }

        return await SubmitScalableAsync(rule, requestId, ct);
    }

    /// <summary>
    /// Current in-memory bootstrap flow — unchanged. Runs under shared ownership.
    /// </summary>
    private async Task<BootstrapRequestResult> SubmitInMemoryAsync(
        string ruleName, Guid requestId, CancellationToken ct)
    {
        try
        {
            // One-shot watchdog: if process crashes between create and enqueue, this job
            // re-schedules bootstrap only while status is still pending_enqueue.
            await scheduler.ScheduleWatchdogAsync(ruleName, requestId, WatchdogDelay, ct);

            var request = await requestStore.GetAsync(requestId, ct) ?? resultRequest(requestId, ruleName);
            var claimToken = Guid.NewGuid().ToString("N");
            var expectation = CreateRecoveryExpectation(request);
            if (!await requestStore.TryClaimSlotAsync(expectation, claimToken,
                    DateTime.UtcNow - policy.IdleAfter, isRecovery: false, ct))
                throw new InvalidOperationException($"Request {requestId} lost its enqueue claim.");

            await scheduler.ScheduleClaimedAsync(ruleName, requestId, claimToken,
                isRecovery: false, TimeSpan.Zero, ct);

            // Reload the updated request
            var updated = await requestStore.GetAsync(requestId, ct);
            return new BootstrapRequestResult(
                updated ?? BootstrapRequest.New(ruleName) with
                {
                    RequestId = requestId,
                    Status = BootstrapRequestStatus.PendingEnqueue
                },
                true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue bootstrap request {RequestId} for {RuleName}",
                requestId, ruleName);

            var failedRequest = await requestStore.GetAsync(requestId, CancellationToken.None);
            if (failedRequest is not null)
                await requestStore.TryRecordSchedulingFailureAsync(requestId,
                    failedRequest.Status, failedRequest.HangfireJobId ?? string.Empty,
                    failedRequest.ReconcileClaimToken, "BootstrapEnqueueFailed",
                    BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Unable to enqueue bootstrap request",
                    CancellationToken.None);

            throw;
        }
    }

    /// <summary>
    /// Scalable parent-child bootstrap flow. Validates preconditions, creates bootstrap_parent,
    /// and enqueues the parent start job.
    /// </summary>
    private async Task<BootstrapRequestResult> SubmitScalableAsync(
        TableMappingRule rule, Guid requestId, CancellationToken ct)
    {
        try
        {
            // Validate scalable preconditions before creating any work
            var preflightResult = await ValidateScalablePreconditionsAsync(rule, ct);
            if (!preflightResult.IsValid)
            {
                // Release shared ownership so subsequent requests are not blocked
                await requestStore.TryFailAsync(requestId, BootstrapRequestStatus.PendingEnqueue,
                    string.Empty, preflightResult.ErrorCode ?? "ScalablePreflightFailed",
                    preflightResult.ErrorMessage ?? "Scalable bootstrap preflight failed.",
                    CancellationToken.None);

                var failed = await requestStore.GetAsync(requestId, ct);
                return new BootstrapRequestResult(
                    failed ?? BootstrapRequest.NewScalable(rule.RuleName) with
                    {
                        RequestId = requestId,
                        Status = BootstrapRequestStatus.Failed,
                        ErrorCode = preflightResult.ErrorCode,
                        ErrorMessage = preflightResult.ErrorMessage
                    },
                    true);
            }

            var activeParent = await parentStore.GetByRuleNameAsync(rule.RuleName, ct);
            if (activeParent is not null && IsActiveParentStatus(activeParent.Status))
            {
                var recovered = await TryFailAbandonedPendingParentAsync(activeParent, ct);
                if (!recovered)
                {
                    var message =
                        $"Rule '{rule.RuleName}' already has active scalable bootstrap parent {activeParent.ParentId} " +
                        $"in status '{activeParent.Status}'.";
                    await requestStore.TryFailAsync(requestId, BootstrapRequestStatus.PendingEnqueue,
                        string.Empty, "ActiveScalableBootstrapExists", message, CancellationToken.None);

                    var blocked = await requestStore.GetAsync(requestId, ct);
                    return new BootstrapRequestResult(
                        blocked ?? BootstrapRequest.NewScalable(rule.RuleName) with
                        {
                            RequestId = requestId,
                            Status = BootstrapRequestStatus.Failed,
                            ErrorCode = "ActiveScalableBootstrapExists",
                            ErrorMessage = message
                        },
                        true);
                }
            }

            // Generate staging table name from a new parent GUID
            var stagingTableName = $"bs_{Guid.NewGuid():N}".ToLowerInvariant();

            // Create durable bootstrap_parent with staging identity
            var parent = await parentStore.CreateAsync(
                rule.RuleName, rule.RuleName,
                rule.Target.Schema, rule.Target.Table,
                stagingTableName, requestId, ct);

            // One-shot watchdog for the parent start job
            await scheduler.ScheduleWatchdogAsync(rule.RuleName, requestId, WatchdogDelay, ct);

            // Enqueue the scalable parent start job
            var request = await requestStore.GetAsync(requestId, ct) ?? resultRequest(requestId, rule.RuleName);
            var claimToken = Guid.NewGuid().ToString("N");
            if (!await requestStore.TryClaimSlotAsync(CreateRecoveryExpectation(request), claimToken,
                    DateTime.UtcNow - policy.IdleAfter, isRecovery: false, ct))
                throw new InvalidOperationException($"Request {requestId} lost its enqueue claim.");
            await scheduler.ScheduleClaimedAsync(rule.RuleName, requestId, claimToken,
                isRecovery: false, TimeSpan.Zero, ct);

            var updated = await requestStore.GetAsync(requestId, ct);
            return new BootstrapRequestResult(
                updated ?? BootstrapRequest.NewScalable(rule.RuleName) with
                {
                    RequestId = requestId,
                    Status = BootstrapRequestStatus.PendingEnqueue
                },
                true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit scalable bootstrap for {RuleName}",
                rule.RuleName);

            var failedRequest = await requestStore.GetAsync(requestId, CancellationToken.None);
            if (failedRequest is not null)
                await requestStore.TryFailAsync(requestId,
                    failedRequest.Status, failedRequest.HangfireJobId ?? string.Empty,
                    "ScalableBootstrapEnqueueFailed",
                    BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Unable to enqueue scalable bootstrap request",
                    CancellationToken.None);

            throw;
        }
    }

    /// <summary>
    /// Validates preconditions for scalable bootstrap.
    /// Returns <see cref="PreflightResult"/> with <c>IsValid = true</c> when all checks pass.
    /// On failure, includes a safe error code and message.
    /// </summary>
    private async Task<PreflightResult> ValidateScalablePreconditionsAsync(
        TableMappingRule rule, CancellationToken ct)
    {
        // Primary key must exist on the source table
        if (rule.Source.PrimaryKey.Count == 0)
        {
            return PreflightResult.Fail(
                "MissingPrimaryKey",
                $"Rule '{rule.RuleName}' has no source primary key. Scalable bootstrap requires a primary key.");
        }

        if (rule.Target.PrimaryKey.Count == 0)
        {
            return PreflightResult.Fail(
                "MissingTargetPrimaryKey",
                $"Rule '{rule.RuleName}' has no target primary key. Scalable bootstrap requires a target primary key.");
        }

        // Validate that the source table is CT-enabled
        // (Full CT health check will be added in Task 2)
        var ctCheck = await ValidateCtEnabledAsync(rule, ct);
        if (!ctCheck.IsValid)
            return ctCheck;

        // Validate sync_meta schema privileges
        // (CREATE privilege check will be added when the infrastructure is available)

        return PreflightResult.Valid();
    }

    /// <summary>
    /// Lightweight CT validation — verifies the source table has a primary key
    /// and basic CT support. Full CT health check (sys.change_tracking_tables)
    /// is added in Task 2.
    /// </summary>
    private async Task<PreflightResult> ValidateCtEnabledAsync(
        TableMappingRule rule, CancellationToken ct)
    {
        // Basic validation: primary key must exist for CT
        if (rule.Source.PrimaryKey.Count == 0)
        {
            return PreflightResult.Fail(
                "CtDisabled",
                $"Source table '{rule.Source.PrimaryTable}' has no primary key. " +
                "Change Tracking requires a primary key.");
        }

        // Defer full CT health check to ISqlServerCtHealthCheck (Task 2)
        return PreflightResult.Valid();
    }

    /// <summary>
    /// Returns the current state of a bootstrap request, or null if it does not exist.
    /// </summary>
    public async Task<BootstrapRequest?> GetStatusAsync(
        Guid requestId, CancellationToken ct)
    {
        // requestId is a struct — always valid. Delegate to store.
        return await requestStore.GetAsync(requestId, ct);
    }

    /// <summary>
    /// Reconciled a single request in the given mode.
    /// <list type="bullet">
    ///   <item><b>OneShot:</b> handles only PendingEnqueue and Queued. Running/WaitingForLock are always no-op.</item>
    ///   <item><b>StaleScan:</b> handles PendingEnqueue and Queued unconditionally. Running/WaitingForLock
    ///         are only probed when the request has exceeded its configured stale threshold.</item>
    /// </list>
    /// </summary>
    public async Task ReconcileOneAsync(
        string ruleName, Guid requestId,
        BootstrapReconciliationContext context, CancellationToken ct)
    {
        var request = await requestStore.GetAsync(requestId, ct);
        if (request is null)
        {
            logger.LogDebug(
                "Watchdog skipped missing bootstrap request {RequestId}", requestId);
            return;
        }

        // Defensive: request row may have been reused under a different rule name.
        if (!string.Equals(request.SourceTable, ruleName, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Watchdog rule-name mismatch for {RequestId}: expected {Expected}, actual {Actual}",
                requestId, ruleName, request.SourceTable);
            ruleName = request.SourceTable;
        }

        if (string.Equals(request.BootstrapType, BootstrapRequestType.Scalable,
                StringComparison.OrdinalIgnoreCase))
        {
            await ReconcileScalableRequestAsync(ruleName, request, context, ct);
            return;
        }

        switch (request.Status)
        {
            case BootstrapRequestStatus.PendingEnqueue:
                await ReconcilePendingEnqueueOneAsync(ruleName, request, context, ct);
                return;

            case BootstrapRequestStatus.Queued:
                // OneShot probes queued requests immediately; StaleScan retains the
                // SQL boundary and defensively rejects any fresh row returned by it.
                if (context.Mode == BootstrapReconciliationMode.StaleScan
                    && request.UpdatedAt > context.ObservedAtUtc - policy.IdleAfter)
                {
                    logger.LogDebug(
                        "StaleScan defers fresh Queued request {RequestId}", requestId);
                    return;
                }
                await ReconcileActiveJobAsync(ruleName, request, context, ct);
                return;

            case BootstrapRequestStatus.Running:
            case BootstrapRequestStatus.WaitingForLock:
                if (context.Mode == BootstrapReconciliationMode.OneShot)
                {
                    logger.LogDebug(
                        "Watchdog (OneShot) no-ops for {RequestId}: status is {Status}",
                        requestId, request.Status);
                    return;
                }

                if (!IsStale(request, context.ObservedAtUtc))
                {
                    logger.LogDebug(
                        "StaleScan defers fresh {Status} request {RequestId}",
                        request.Status, requestId);
                    return;
                }

                await ReconcileActiveJobAsync(ruleName, request, context, ct);
                return;

            default:
                logger.LogDebug(
                    "Watchdog no-op for {RequestId}: status is {Status}",
                    requestId, request.Status);
                return;
        }
    }

    private async Task ReconcileScalableRequestAsync(
        string ruleName,
        BootstrapRequest request,
        BootstrapReconciliationContext context,
        CancellationToken ct)
    {
        var parent = await parentStore.GetByRuleNameAsync(ruleName, ct);
        if (parent is null || parent.BootstrapRequestId != request.RequestId)
        {
            await requestStore.TryMarkRecoveryFailedAsync(CreateRecoveryExpectation(request),
                "ScalableBootstrapParentMissing",
                $"No scalable bootstrap parent linked to request {request.RequestId} for rule '{ruleName}'.", ct);
            return;
        }

        switch (BootstrapParentRecoveryClassifier.Classify(parent))
        {
            case BootstrapParentRecoveryAction.StartPending:
                await ReconcileScalablePendingParentAsync(ruleName, request, context, ct);
                return;
            case BootstrapParentRecoveryAction.ResumeRunning:
            case BootstrapParentRecoveryAction.ResumeCatchingUp:
            case BootstrapParentRecoveryAction.ResumePublishing:
                await ReconcileScalableActiveParentAsync(ruleName, request, parent, context, ct);
                return;
            case BootstrapParentRecoveryAction.RecoveryPending:
                logger.LogInformation("Scalable parent {ParentId} is already recovery-pending; request {RequestId} no-ops", parent.ParentId, request.RequestId);
                return;
            case BootstrapParentRecoveryAction.SyncCompleted:
                await requestStore.TryCompleteAsync(request.RequestId, request.Status,
                    request.HangfireJobId ?? string.Empty, ct);
                return;
            case BootstrapParentRecoveryAction.SyncFailed:
                await requestStore.TryFailAsync(request.RequestId, request.Status,
                    request.HangfireJobId ?? string.Empty,
                    parent.ErrorCode ?? "ScalableBootstrapParentFailed",
                    BootstrapDiagnosticSanitizer.Sanitize(parent.ErrorMessage) ??
                        $"Scalable parent {parent.ParentId} ended in status '{parent.Status}'.", ct);
                return;
            default:
                await requestStore.TryMarkRecoveryFailedAsync(CreateRecoveryExpectation(request),
                    "ScalableBootstrapParentStateUnknown",
                    $"Scalable parent {parent.ParentId} has unsupported status '{parent.Status}'.", ct);
                return;
        }
    }

    private async Task ReconcileScalablePendingParentAsync(
        string ruleName, BootstrapRequest request, BootstrapReconciliationContext context, CancellationToken ct)
    {
        if (string.Equals(request.Status, BootstrapRequestStatus.PendingEnqueue, StringComparison.OrdinalIgnoreCase))
        {
            await ReconcilePendingEnqueueOneAsync(ruleName, request, context, ct);
            return;
        }

        if (!string.Equals(request.Status, BootstrapRequestStatus.Queued, StringComparison.OrdinalIgnoreCase))
            return;

        if (context.Mode == BootstrapReconciliationMode.StaleScan
            && request.UpdatedAt > context.ObservedAtUtc - policy.IdleAfter)
            return;

        var observed = jobStateChecker.Probe(request.HangfireJobId);
        if (observed.Kind is BootstrapJobStateKind.Alive or BootstrapJobStateKind.Unknown)
            return;
        if (observed.Kind == BootstrapJobStateKind.TerminalSuccess)
        {
            await TryFailInconsistentStateAsync(ruleName, request, observed, ct);
            return;
        }

        await RecoverOrFailAsync(ruleName, request, observed, context, ct);
    }

    private async Task ReconcileScalableActiveParentAsync(
        string ruleName, BootstrapRequest request, BootstrapParent parent,
        BootstrapReconciliationContext context, CancellationToken ct)
    {
        if (request.Status is BootstrapRequestStatus.PendingEnqueue or BootstrapRequestStatus.Queued)
        {
            await requestStore.TryMarkRunningAsync(request.RequestId, request.Status,
                request.HangfireJobId ?? string.Empty, ct);
            return;
        }

        if (context.Mode == BootstrapReconciliationMode.OneShot || !IsStale(request, context.ObservedAtUtc))
            return;

        var action = BootstrapParentRecoveryClassifier.Classify(parent);
        switch (action)
        {
            case BootstrapParentRecoveryAction.ResumeRunning:
                await ReconcileScalableRunningParentAsync(ruleName, request, parent, context, ct);
                return;
            case BootstrapParentRecoveryAction.ResumeCatchingUp:
            case BootstrapParentRecoveryAction.ResumePublishing:
                await ReconcileScalableFinalizePhaseParentAsync(ruleName, request, parent, context, ct);
                return;
            case BootstrapParentRecoveryAction.SyncCompleted:
                await requestStore.TryCompleteAsync(request.RequestId, request.Status,
                    request.HangfireJobId ?? string.Empty, ct);
                return;
            case BootstrapParentRecoveryAction.SyncFailed:
                await requestStore.TryFailAsync(request.RequestId, request.Status,
                    request.HangfireJobId ?? string.Empty,
                    parent.ErrorCode ?? "ScalableBootstrapParentFailed",
                    BootstrapDiagnosticSanitizer.Sanitize(parent.ErrorMessage) ??
                        $"Scalable parent {parent.ParentId} ended in status '{parent.Status}'.", ct);
                return;
            default:
                logger.LogDebug("Scalable parent {ParentId} action {Action} — no-op",
                    parent.ParentId, action);
                return;
        }
    }

    /// <summary>
    /// Running-phase recovery via child-chain inspection. Never probes PhaseJobId
    /// and never goes directly to finalize scheduling.
    /// </summary>
    private async Task ReconcileScalableRunningParentAsync(
        string ruleName, BootstrapRequest request, BootstrapParent parent,
        BootstrapReconciliationContext context, CancellationToken ct)
    {
        if (parent.LastHeartbeatAt is not null
            && parent.LastHeartbeatAt > context.ObservedAtUtc - policy.RunningStaleAfter)
            return;

        var children = await childStore.GetByParentAsync(parent.ParentId, ct);

        var failed = children.LastOrDefault(c => c.Status == BootstrapChildStatus.Failed);
        if (failed is not null)
        {
            var evidence = BootstrapDiagnosticSanitizer.Sanitize(
                $"Child {failed.ChildId} for parent {parent.ParentId} is Failed. " +
                $"Error: {failed.ErrorCode} — {failed.ErrorMessage}")
                ?? "A child bootstrap job has failed.";
            var terminalized = await requestStore.TryFailScalableChildRecoveryAsync(
                CreateRecoveryExpectation(request),
                new BootstrapChildFailureExpectation(failed.ChildId, failed.ParentId,
                    failed.Status, failed.HangfireJobId),
                parent.ParentId, parent.FencingToken,
                parent.Status, parent.LastHeartbeatAt, parent.PhaseJobId,
                failed.ErrorCode ?? "ChildFailed", evidence, ct);
            if (!terminalized)
            {
                logger.LogDebug(
                    "Child failure terminalization CAS lost for parent {ParentId}, request {RequestId}",
                    parent.ParentId, request.RequestId);
            }
            return;
        }

        var activeChild = children.LastOrDefault(c =>
            c.Status is BootstrapChildStatus.PendingEnqueue
                or BootstrapChildStatus.Queued
                or BootstrapChildStatus.Running);
        if (activeChild is not null)
        {
            await RecoverScalableChildIfTerminalAsync(ruleName, request, parent, activeChild, context, ct);
            return;
        }

        var latestChild = children.LastOrDefault();
        if (latestChild is null || latestChild.Status != BootstrapChildStatus.Completed)
            return;

        var isEof = latestChild.RowsRead < BootstrapChildService.DefaultBatchSize;
        if (isEof)
        {
            await RecoverScalableLostFinalizeAsync(ruleName, request, parent, context, ct);
        }
        else
        {
            await RecoverScalableLostNextChildAsync(ruleName, parent, latestChild, ct);
        }
    }

    private async Task RecoverScalableChildIfTerminalAsync(
        string ruleName, BootstrapRequest request, BootstrapParent parent, BootstrapChild activeChild,
        BootstrapReconciliationContext context, CancellationToken ct)
    {
        var state = jobStateChecker.Probe(activeChild.HangfireJobId);
        if (state.Kind is BootstrapJobStateKind.Alive or BootstrapJobStateKind.Unknown)
            return;

        if (activeChild.Status == BootstrapChildStatus.Running
            && state.Kind is not BootstrapJobStateKind.Missing
                and not BootstrapJobStateKind.TerminalFailure)
            return;

        // The request is the recovery budget owner. Re-enter through the normal
        // claimed dispatcher so its counter and timestamps are finalized before
        // it resumes the fenced child chain.
        await RecoverOrFailAsync(ruleName, request, state, context, ct);
    }

    /// <summary>
    /// Lost-finalize recovery: all children completed and EOF reached but no finalize.
    /// Claims request recovery + parent phase job atomically, then schedules finalize.
    /// </summary>
    private async Task RecoverScalableLostFinalizeAsync(
        string ruleName, BootstrapRequest request, BootstrapParent parent,
        BootstrapReconciliationContext context, CancellationToken ct)
    {
        var expectation = CreateRecoveryExpectation(request);
        if (request.ReconcileAttemptCount >= policy.MaxRecoveryAttempts)
        {
            var evidence = BootstrapDiagnosticSanitizer.Sanitize(
                $"Scalable bootstrap recovery exhausted (lost finalize). requestId={request.RequestId}; " +
                $"rule={ruleName}; parentId={parent.ParentId}; attempts={request.ReconcileAttemptCount}; " +
                $"maximum={policy.MaxRecoveryAttempts}. Manual retry is required.")
                ?? "Scalable bootstrap recovery exhausted. Manual retry is required.";
            await requestStore.TryFailScalableRecoveryExhaustedAsync(expectation, parent.ParentId,
                parent.FencingToken, parent.Status, parent.LastHeartbeatAt, parent.PhaseJobId,
                "BootstrapJobRecoveryExhausted", evidence, ct);
            return;
        }

        var staleBefore = context.ObservedAtUtc - policy.IdleAfter;
        var token = Guid.NewGuid().ToString("N");
        var parentExpectation = new BootstrapParentPhaseJobExpectation(
            parent.ParentId, parent.FencingToken, BootstrapParentStatus.Running,
            parent.PhaseJobId, token, staleBefore);
        if (!await requestStore.TryClaimScalableRecoveryAsync(expectation, parentExpectation,
                token, isRecovery: true, ct))
            return;

        try
        {
            await scheduler.ScheduleClaimedScalableRecoveryFinalizeAsync(ruleName, request.RequestId,
                parent.ParentId, parent.FencingToken, BootstrapParentStatus.Running,
                parent.PhaseJobId, token, ct);
        }
        catch (Exception ex)
        {
            await FailOwnedPhaseClaimAsync(ruleName, request, parent, token,
                BootstrapParentStatus.Running, ex, context.ObservedAtUtc, ct);
        }
    }

    /// <summary>
    /// Idempotent lost-next-child recovery using TryCreateNextChildAsync.
    /// </summary>
    private async Task RecoverScalableLostNextChildAsync(
        string ruleName, BootstrapParent parent, BootstrapChild latestChild,
        CancellationToken ct)
    {
        var nextChild = await childStore.TryCreateNextChildAsync(
            parent.ParentId, parent.FencingToken,
            latestChild.Sequence, latestChild.LastKey,
            latestChild.LastKey, ct);

        if (!nextChild.WasCreated)
            return;

        try
        {
            var token = Guid.NewGuid().ToString("N");
            if (!await childStore.TryClaimInitialAsync(nextChild.Child.ChildId, parent.ParentId,
                    parent.FencingToken, token, ct))
                return;
            await scheduler.ScheduleClaimedChildAsync(ruleName, parent.ParentId,
                nextChild.Child.ChildId, parent.FencingToken, BootstrapChildStatus.PendingEnqueue,
                token, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to enqueue newly-created child {ChildId} for parent {ParentId}; reconciliation will retry",
                nextChild.Child.ChildId, parent.ParentId);
        }
    }

    /// <summary>
    /// CatchingUp/Publishing phase recovery: probes durable phase job, evaluates
    /// recovery eligibility, and schedules finalize with recovery accounting.
    /// </summary>
    private async Task ReconcileScalableFinalizePhaseParentAsync(
        string ruleName, BootstrapRequest request, BootstrapParent parent,
        BootstrapReconciliationContext context, CancellationToken ct)
    {
        var phaseJobObserved = jobStateChecker.Probe(parent.PhaseJobId);
        switch (phaseJobObserved.Kind)
        {
            case BootstrapJobStateKind.Alive:
                logger.LogDebug("Phase job {PhaseJobId} for parent {ParentId} is alive — no-op",
                    parent.PhaseJobId, parent.ParentId);
                return;
            case BootstrapJobStateKind.Unknown:
                logger.LogWarning("Phase job {PhaseJobId} for parent {ParentId} is unknown — no-op",
                    parent.PhaseJobId, parent.ParentId);
                return;
            case BootstrapJobStateKind.TerminalSuccess:
                if (parent.PhaseClaimToken is not null
                    && parent.PhaseClaimedAt is not null
                    && parent.PhaseClaimedAt > context.ObservedAtUtc - policy.IdleAfter)
                {
                    logger.LogDebug(
                        "Phase job {PhaseJobId} Succeeded but parent {ParentId} has fresh claim — replacement in-flight",
                        parent.PhaseJobId, parent.ParentId);
                    return;
                }
                await TryFailInconsistentPhaseStateAsync(ruleName, request, parent,
                    phaseJobObserved, context.ObservedAtUtc, ct);
                return;
        }

        var expectation = CreateRecoveryExpectation(request);
        if (request.ReconcileAttemptCount >= policy.MaxRecoveryAttempts)
        {
            var evidence = BootstrapDiagnosticSanitizer.Sanitize(
                $"Scalable bootstrap recovery exhausted. requestId={request.RequestId}; rule={ruleName}; " +
                $"parentId={parent.ParentId}; parentPhase={parent.Status}; attempts={request.ReconcileAttemptCount}; " +
                $"maximum={policy.MaxRecoveryAttempts}. Manual retry is required.")
                ?? "Scalable bootstrap recovery exhausted. Manual retry is required.";
            await requestStore.TryFailScalableRecoveryExhaustedAsync(expectation, parent.ParentId,
                parent.FencingToken, parent.Status, parent.LastHeartbeatAt, parent.PhaseJobId,
                "BootstrapJobRecoveryExhausted", evidence, ct);
            return;
        }

        var staleBefore = context.ObservedAtUtc - policy.IdleAfter;
        var token = Guid.NewGuid().ToString("N");
        var parentExpectation = new BootstrapParentPhaseJobExpectation(
            parent.ParentId, parent.FencingToken, parent.Status,
            parent.PhaseJobId, token, staleBefore);
        if (!await requestStore.TryClaimScalableRecoveryAsync(expectation, parentExpectation,
                token, isRecovery: true, ct))
            return;

        try
        {
            await scheduler.ScheduleClaimedAsync(ruleName, request.RequestId, token,
                isRecovery: true, TimeSpan.Zero, ct);
            logger.LogInformation(
                "Claimed scalable recovery finalized for {RequestId} {RuleName} (phase={Phase})",
                request.RequestId, ruleName, parent.Status);
        }
        catch (Exception ex)
        {
            await FailOwnedPhaseClaimAsync(ruleName, request, parent, token,
                parent.Status, ex, context.ObservedAtUtc, ct);
        }
    }

    private async Task TryFailInconsistentPhaseStateAsync(
        string ruleName, BootstrapRequest request, BootstrapParent parent,
        BootstrapJobStateSnapshot observed, DateTime observedAtUtc, CancellationToken ct)
    {
        var timestamp = observed.ObservedAt?.ToString("O") ?? observedAtUtc.ToString("O");
        var message =
            $"Hangfire phase job {parent.PhaseJobId} reports Succeeded while parent {parent.ParentId} " +
            $"for rule '{ruleName}' is still {parent.Status}. " +
            $"Observed state: {observed.State ?? "Succeeded"}. " +
            $"ObservedAt: {timestamp}. " +
            $"Phase claim token: {(parent.PhaseClaimToken is not null ? "present" : "null")}. " +
            $"Phase claimedAt: {parent.PhaseClaimedAt:O}. Manual investigation is required.";
        message = message.Length <= MaxPersistedErrorLength ? message : message[..MaxPersistedErrorLength];

        var claimExpiredAt = observedAtUtc - policy.IdleAfter;
        var succeeded = await requestStore.TryFailInconsistentPhaseStateAsync(
            CreateRecoveryExpectation(request),
            parent.ParentId, parent.FencingToken,
            parent.Status, parent.LastHeartbeatAt, parent.PhaseJobId,
            claimExpiredAt,
            "BootstrapStateInconsistent",
            message,
            ct);

        if (succeeded)
        {
            logger.LogError(
                "Bootstrap phase state inconsistent for parent {ParentId} {RuleName}: phase job Succeeded but parent was {Status}",
                parent.ParentId, ruleName, parent.Status);
        }
    }

    /// <summary>
    /// Unified claim protocol for PendingEnqueue recovery: claim the request,
    /// then enqueue a job. Only the claim holder proceeds to enqueue.
    /// PendingEnqueue is not a recovery, so isRecovery=false (no attempt/timestamps).
    /// </summary>
    private async Task ReconcilePendingEnqueueOneAsync(
        string ruleName,
        BootstrapRequest request,
        BootstrapReconciliationContext context,
        CancellationToken ct)
    {
        var expectation = CreateRecoveryExpectation(request);
        var claimToken = Guid.NewGuid().ToString("N");
        var staleClaimBeforeUtc = context.ObservedAtUtc - policy.IdleAfter;
        var claimed = await requestStore.TryClaimSlotAsync(
            expectation, claimToken, staleClaimBeforeUtc, isRecovery: false, ct);
        if (!claimed)
        {
            logger.LogDebug(
                "PendingEnqueue claim CAS lost for {RequestId} — another reconciler already claimed",
                request.RequestId);
            return;
        }

        try
        {
            await scheduler.ScheduleClaimedAsync(
                ruleName, request.RequestId, claimToken, isRecovery: false, TimeSpan.Zero, ct);
            logger.LogInformation(
                "Claimed bootstrap job scheduled for pending request {RequestId} for {RuleName}; waiting for job-side finalize",
                request.RequestId, ruleName);
        }
        catch (Exception ex)
        {
            await FailOwnedClaimAsync(
                ruleName, request, claimToken, "PendingEnqueue", ex, context.ObservedAtUtc, ct);
        }
    }

    /// <summary>
    /// Recovery path for a request whose Hangfire job may have become terminal
    /// or missing. Probes the job via the classified state checker and routes
    /// to the appropriate recovery branch: alive jobs are left untouched;
    /// terminal/missing jobs are recovered up to 3 times; terminal success
    /// with a queued request is treated as an invariant violation.
    /// </summary>
    private async Task ReconcileActiveJobAsync(
        string ruleName,
        BootstrapRequest request,
        BootstrapReconciliationContext context,
        CancellationToken ct)
    {
        var observed = jobStateChecker.Probe(request.HangfireJobId);
        switch (observed.Kind)
        {
            case BootstrapJobStateKind.Alive:
                logger.LogDebug(
                    "Watchdog no-op for {RequestId}: job {JobId} is alive ({State})",
                    request.RequestId, request.HangfireJobId, observed.State);
                return;
            case BootstrapJobStateKind.Unknown:
                logger.LogWarning(
                    "Watchdog no-op for {RequestId}: job {JobId} is in unknown state ({State})",
                    request.RequestId, request.HangfireJobId, observed.State);
                return;
            case BootstrapJobStateKind.TerminalSuccess:
                await TryFailInconsistentStateAsync(ruleName, request, observed, ct);
                return;
            case BootstrapJobStateKind.TerminalFailure:
            case BootstrapJobStateKind.Missing:
                await RecoverOrFailAsync(ruleName, request, observed, context, ct);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(observed.Kind));
        }
    }

    /// <summary>
    /// Marks a queued request as failed when Hangfire reports Succeeded —
    /// an invariant violation that must not re-execute bootstrap.
    /// </summary>
    private async Task TryFailInconsistentStateAsync(
        string ruleName, BootstrapRequest request,
        BootstrapJobStateSnapshot observed, CancellationToken ct)
    {
        var timestamp = observed.ObservedAt?.ToString("O") ?? DateTime.UtcNow.ToString("O");
        var serverInfo = observed.ServerId is not null ? $"Server: {observed.ServerId}. " : "Server: <unknown>. ";
        var message =
            $"Hangfire job {request.HangfireJobId} reports Succeeded while the durable request {request.RequestId} " +
            $"for rule '{ruleName}' is still {request.Status}. " +
            $"Observed state: {observed.State ?? "Succeeded"}. " +
            $"{serverInfo}" +
            $"ObservedAt: {timestamp}. " +
            $"Automatic retry was intentionally suppressed to avoid duplicate side effects. Manual investigation is required.";
        message = message.Length <= MaxPersistedErrorLength ? message : message[..MaxPersistedErrorLength];

        var succeeded = await requestStore.TryMarkRecoveryFailedAsync(
            CreateRecoveryExpectation(request),
            "BootstrapStateInconsistent",
            message,
            ct);

        if (succeeded)
        {
            logger.LogError(
                "Bootstrap state inconsistent for {RequestId} {RuleName}: Hangfire Succeeded but request was {Status}",
                request.RequestId, ruleName, request.Status);
        }
        else
        {
            logger.LogDebug(
                "TryFailInconsistentStateAsync CAS lost for {RequestId} — another reconciler already handled it",
                request.RequestId);
        }
    }

    /// <summary>
    /// Unified recovery protocol: claim first (no attempt/timestamps), then enqueue
    /// a fenced recovery job that self-finalizes. Attempt count, first_recovery_at,
    /// and last_recovery_at are only set at successful finalize (by the job).
    /// CAS losers exit immediately without creating orphan Hangfire jobs.
    /// When the recovery limit (3) is reached, marks the request permanently failed.
    /// </summary>
    private async Task RecoverOrFailAsync(
        string ruleName,
        BootstrapRequest request,
        BootstrapJobStateSnapshot observed,
        BootstrapReconciliationContext context,
        CancellationToken ct)
    {
        if (request.ReconcileAttemptCount >= policy.MaxRecoveryAttempts)
        {
            var marked = await requestStore.TryMarkRecoveryFailedAsync(
                CreateRecoveryExpectation(request),
                "BootstrapJobRecoveryExhausted",
                BuildRecoveryFailureMessage(ruleName, request, observed),
                ct);

            if (!marked)
            {
                logger.LogDebug(
                    "TryMarkRecoveryFailedAsync CAS lost for {RequestId} — already handled by another reconciler",
                    request.RequestId);
            }

            return;
        }

        var expectation = CreateRecoveryExpectation(request);
        var claimToken = Guid.NewGuid().ToString("N");
        var staleClaimBeforeUtc = context.ObservedAtUtc - policy.IdleAfter;
        var claimed = await requestStore.TryClaimSlotAsync(
            expectation, claimToken, staleClaimBeforeUtc, isRecovery: true, ct);
        if (!claimed)
        {
            logger.LogDebug(
                "Recovery claim CAS lost for {RequestId} — another reconciler won",
                request.RequestId);
            return;
        }

        try
        {
            await scheduler.ScheduleClaimedAsync(
                ruleName, request.RequestId, claimToken, isRecovery: true, TimeSpan.Zero, ct);
            logger.LogInformation(
                "Claimed recovery job scheduled for {RequestId} {RuleName}; waiting for job-side finalize",
                request.RequestId, ruleName);
        }
        catch (Exception ex)
        {
            await FailOwnedClaimAsync(
                ruleName, request, claimToken, observed.State ?? "Missing", ex, context.ObservedAtUtc, ct);
        }
    }

    private async Task FailOwnedClaimAsync(
        string ruleName,
        BootstrapRequest request,
        string claimToken,
        string phase,
        Exception exception,
        DateTime observedAtUtc,
        CancellationToken ct)
    {
        var evidence = BootstrapDiagnosticSanitizer.Sanitize(
            $"Bootstrap claimed-job scheduling failed. " +
            $"RequestId: {request.RequestId}. " +
            $"Rule: {ruleName}. " +
            $"Phase: {phase}. " +
            $"OldJobId: {request.HangfireJobId ?? "<null>"}. " +
            $"Exception: {exception.GetType().Name}. " +
            $"Reason: {exception.Message}. " +
            $"ObservedAt: {observedAtUtc:O}. " +
            $"Attempt: {request.ReconcileAttemptCount}/{policy.MaxRecoveryAttempts}. " +
            "Manual retry may be required.") ?? "Bootstrap claimed-job scheduling failed.";
        evidence = evidence.Length <= MaxPersistedErrorLength
            ? evidence
            : evidence[..MaxPersistedErrorLength];

        logger.LogError(exception,
            "Claimed bootstrap job scheduling failed for {RequestId} {RuleName} during {Phase}",
            request.RequestId, ruleName, phase);

        var failed = await requestStore.TryRecordSchedulingFailureAsync(
            request.RequestId, request.Status, request.HangfireJobId ?? string.Empty,
            claimToken, "BootstrapJobRecoveryEnqueueFailed", evidence, ct);
        if (!failed)
        {
            logger.LogDebug(
                "Scheduling-failure CAS lost for {RequestId} — another reconciler already handled it",
                request.RequestId);
        }
    }

    /// <summary>
    /// Fails the parent phase claim using the parent's own CAS (fencing token, status,
    /// phase_claim_token), NOT the request claim token. Also releases the request claim
    /// when scheduling fails after an atomic scalable recovery claim.
    /// </summary>
    private async Task FailOwnedPhaseClaimAsync(
        string ruleName,
        BootstrapRequest request,
        BootstrapParent parent,
        string claimToken,
        string phase,
        Exception exception,
        DateTime observedAtUtc,
        CancellationToken ct)
    {
        var evidence = BootstrapDiagnosticSanitizer.Sanitize(
            $"Scalable bootstrap claimed-phase-job scheduling failed. " +
            $"ParentId: {parent.ParentId}. " +
            $"Rule: {ruleName}. " +
            $"Phase: {phase}. " +
            $"RequestId: {request.RequestId}. " +
            $"OldPhaseJobId: {parent.PhaseJobId ?? "<null>"}. " +
            $"Exception: {exception.GetType().Name}. " +
            $"Reason: {exception.Message}. " +
            $"ObservedAt: {observedAtUtc:O}. " +
            "Manual retry may be required.") ?? "Scalable bootstrap claimed-phase-job scheduling failed.";
        evidence = evidence.Length <= MaxPersistedErrorLength
            ? evidence
            : evidence[..MaxPersistedErrorLength];

        logger.LogError(exception,
            "Claimed phase job scheduling failed for parent {ParentId} {RuleName} during {Phase}",
            parent.ParentId, ruleName, phase);

        var failed = await requestStore.TryRecordScalablePhaseSchedulingFailureAsync(
            new BootstrapRecoveryExpectation(request.RequestId, request.Status,
                request.HangfireJobId ?? string.Empty, request.ReconcileAttemptCount),
            new BootstrapParentPhaseJobExpectation(parent.ParentId, parent.FencingToken,
                parent.Status, parent.PhaseJobId, claimToken, DateTime.MinValue),
            "ScalableBootstrapPhaseEnqueueFailed", evidence, ct);
        if (!failed)
        {
            logger.LogDebug(
                "Scalable recovery failure CAS lost for parent {ParentId} — already handled",
                parent.ParentId);
        }
    }

    private string BuildRecoveryFailureMessage(
        string ruleName, BootstrapRequest request, BootstrapJobStateSnapshot observed)
    {
        var phase = request.Status == BootstrapRequestStatus.Queued
            ? "before execution started"
            : $"while the durable request was '{request.Status}'";
        var timestamp = observed.ObservedAt?.ToString("O") ?? DateTime.UtcNow.ToString("O");
        var serverInfo = observed.ServerId is not null ? $"Server: {observed.ServerId}. " : "Server: <unknown>. ";
        var firstRecovery = request.FirstRecoveryAt?.ToString("O") ?? "<never>";
        var lastRecovery = request.LastRecoveryAt?.ToString("O") ?? "<never>";
        var safeReason = BootstrapDiagnosticSanitizer.Sanitize(
            observed.ExceptionMessage) ?? "Job record was not available";
        var message =
            $"Bootstrap job became terminal or missing {phase} and could not be recovered after " +
            $"{request.ReconcileAttemptCount} attempt(s) (max {policy.MaxRecoveryAttempts}). " +
            $"RequestId: {request.RequestId}. " +
            $"Rule: {ruleName}. " +
            $"Last Hangfire job: {request.HangfireJobId ?? "<missing>"}. " +
            $"Last state: {observed.State ?? "Missing"}. " +
            $"{serverInfo}" +
            $"Exception: {observed.ExceptionType ?? "<none>"}. " +
            $"Reason: {safeReason}. " +
            $"ObservedAt: {timestamp}. " +
            $"FirstRecoveryAt: {firstRecovery}. " +
            $"LastRecoveryAt: {lastRecovery}. " +
            "Manual retry is required.";
        return message.Length <= MaxPersistedErrorLength ? message : message[..MaxPersistedErrorLength];
    }

    private static BootstrapRecoveryExpectation CreateRecoveryExpectation(
        BootstrapRequest request) => new(
            request.RequestId,
            request.Status,
            request.HangfireJobId ?? string.Empty,
            request.ReconcileAttemptCount);

    private static BootstrapRequest resultRequest(Guid requestId, string sourceTable) =>
        BootstrapRequest.New(sourceTable) with { RequestId = requestId };

    private async Task<bool> TryFailAbandonedPendingParentAsync(
        BootstrapParent parent, CancellationToken ct)
    {
        if (!string.Equals(
                parent.Status,
                BootstrapParentStatus.PendingEnqueue,
                StringComparison.OrdinalIgnoreCase)
            || parent.StagingCreatedAt.HasValue
            || !parent.BootstrapRequestId.HasValue)
        {
            return false;
        }

        var request = await requestStore.GetAsync(parent.BootstrapRequestId.Value, ct);
        if (request is null || !IsTerminalRequestStatus(request.Status))
        {
            return false;
        }

        var failed = await parentStore.TryFailAsync(
            parent.ParentId,
            parent.FencingToken,
            "AbandonedPendingParent",
            $"Parent was left pending after bootstrap request {request.RequestId} reached terminal status '{request.Status}'.",
            ct);

        if (failed)
        {
            logger.LogWarning(
                "Recovered abandoned scalable bootstrap parent {ParentId} for {RuleName}; linked request {RequestId} was {Status}",
                parent.ParentId, parent.RuleName, request.RequestId, request.Status);
        }

        return failed;
    }

    private static bool IsActiveParentStatus(string status)
        => string.Equals(status, BootstrapParentStatus.PendingEnqueue, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, BootstrapParentStatus.Running, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, BootstrapParentStatus.CatchingUp, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, BootstrapParentStatus.Publishing, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, BootstrapParentStatus.RecoveryPending, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, BootstrapParentStatus.CancelRequested, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true when an active (Running/WaitingForLock) request is stale relative
    /// to <paramref name="observedAtUtc"/>. Used only by StaleScan.
    /// </summary>
    private bool IsStale(BootstrapRequest request, DateTime observedAtUtc)
    {
        var cutoff = request.Status == BootstrapRequestStatus.Running
            ? observedAtUtc - policy.RunningStaleAfter
            : observedAtUtc - policy.WaitingForLockStaleAfter;
        return request.UpdatedAt <= cutoff;
    }

    private static bool IsTerminalRequestStatus(string status)
        => string.Equals(status, BootstrapRequestStatus.Completed, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, BootstrapRequestStatus.Failed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Batch stale-scan reconciliation. Calculates all cutoffs from <paramref name="observedAtUtc"/>
    /// and the injected policy. Used by recurring job and manual reconciliation API.
    /// Each request is reconciled in StaleScan mode; the service enforces the staleness
    /// guard even when the SQL query already filtered correctly.
    /// </summary>
    public async Task ReconcileStaleAsync(DateTime observedAtUtc, CancellationToken ct)
    {
        var context = new BootstrapReconciliationContext(
            BootstrapReconciliationMode.StaleScan, observedAtUtc);

        var idleCutoff = observedAtUtc - policy.IdleAfter;
        var runningCutoff = observedAtUtc - policy.RunningStaleAfter;
        var waitingForLockCutoff = observedAtUtc - policy.WaitingForLockStaleAfter;

        var stalePending = await requestStore.GetPendingEnqueueBeforeAsync(idleCutoff, ct);
        foreach (var request in stalePending)
        {
            await ReconcileOneAsync(request.SourceTable, request.RequestId, context, ct);
        }

        var staleQueued = await requestStore.GetQueuedBeforeAsync(idleCutoff, ct);
        foreach (var request in staleQueued)
        {
            await ReconcileOneAsync(request.SourceTable, request.RequestId, context, ct);
        }

        var staleRunning = await requestStore.GetStaleActiveBeforeAsync(
            BootstrapRequestStatus.Running, runningCutoff, ct);
        foreach (var request in staleRunning)
        {
            await ReconcileOneAsync(request.SourceTable, request.RequestId, context, ct);
        }

        var staleWaitingForLock = await requestStore.GetStaleActiveBeforeAsync(
            BootstrapRequestStatus.WaitingForLock, waitingForLockCutoff, ct);
        foreach (var request in staleWaitingForLock)
        {
            await ReconcileOneAsync(request.SourceTable, request.RequestId, context, ct);
        }
    }

    /// <summary>
    /// Returns true when a queued request has exceeded the idle threshold
    /// and is eligible for stale inspection.
    /// </summary>
    private bool IsIdle(BootstrapRequest request, DateTime observedAtUtc)
    {
        var cutoff = observedAtUtc - policy.IdleAfter;
        return request.UpdatedAt <= cutoff;
    }
}
