namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Services;
using Application.Features.CentralDbSync.Validation;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

public sealed class CentralDbSyncJobs
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CentralDbSyncJobs> _logger;
    private readonly IMappingRuleProvider _ruleProvider;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IBootstrapConcurrencyManager _concurrencyManager;
    private readonly ICentralDbConnectionFactory _connectionFactory;
    private readonly IBootstrapReconciliationPolicy _reconciliationPolicy;

    public CentralDbSyncJobs(
        IServiceScopeFactory scopeFactory,
        IMappingRuleProvider ruleProvider,
        IBackgroundJobClient backgroundJobClient,
        IBootstrapConcurrencyManager concurrencyManager,
        ICentralDbConnectionFactory connectionFactory,
        IBootstrapReconciliationPolicy reconciliationPolicy,
        ILogger<CentralDbSyncJobs> logger)
    {
        _scopeFactory = scopeFactory;
        _ruleProvider = ruleProvider;
        _backgroundJobClient = backgroundJobClient;
        _concurrencyManager = concurrencyManager;
        _connectionFactory = connectionFactory;
        _reconciliationPolicy = reconciliationPolicy;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    [AutomaticRetry(Attempts = 0)]  // We handle retry internally
    public async Task RunPilotAsync(CancellationToken cancellationToken)
        => await RunAsync(cancellationToken);

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    [AutomaticRetry(Attempts = 0)]  // We handle retry internally
    public async Task RunAsync(CancellationToken cancellationToken)
        => await RunInternalAsync(syncTier: null, cancellationToken);

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    [AutomaticRetry(Attempts = 0)]  // We handle retry internally
    public async Task RunHotAsync(PerformContext performContext, CancellationToken cancellationToken)
        => await RunTierAsync(performContext, "Hot", cancellationToken);

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    [AutomaticRetry(Attempts = 0)]  // We handle retry internally
    public async Task RunColdAsync(PerformContext performContext, CancellationToken cancellationToken)
        => await RunTierAsync(performContext, "Cold", cancellationToken);

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    [AutomaticRetry(Attempts = 0)]  // We handle retry internally
    public async Task RunTierAsync(PerformContext performContext, string syncTier, CancellationToken cancellationToken)
    {
        SyncGuard.AssertValidSyncTier(syncTier, nameof(syncTier));
        var entries = await RunInternalAsync(syncTier, cancellationToken);
        StoreJobParameters(performContext, syncTier, entries);
    }

    private async Task<IReadOnlyList<SyncRunLogEntry>> RunInternalAsync(string? syncTier, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SyncOrchestrator>();
        var ruleProvider = scope.ServiceProvider.GetRequiredService<IMappingRuleProvider>();
        var configStore = scope.ServiceProvider.GetRequiredService<ISyncConfigStore>();

        var configs = ruleProvider
            .GetAll()
            .Where(rule => rule.Enabled && BelongsToTier(rule, syncTier))
            .Select(rule => rule.ToTableSyncConfig())
            .ToArray();

        // Filter out tables that have been explicitly disabled at runtime
        var enabled = new List<TableSyncConfig>(configs.Length);
        foreach (var config in configs)
        {
            if (await configStore.IsEnabledAsync(config.SourceTable, cancellationToken))
                enabled.Add(config);
            else
                _logger.LogDebug(
                    "Skipping disabled table {SourceTable} in central DB sync tier {SyncTier}",
                    config.SourceTable,
                    syncTier ?? "All");
        }

        _logger.LogDebug(
            "Running central DB sync tier {SyncTier} for {TableCount} registered table(s).",
            syncTier ?? "All",
            enabled.Count);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);
        return await orchestrator.ExecuteAsync(enabled.ToArray(), linkedCts.Token);
    }

    private static void StoreJobParameters(
        PerformContext performContext,
        string? syncTier,
        IReadOnlyList<SyncRunLogEntry> entries)
    {
        if (entries.Count == 0)
            return;

        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Store a compact summary as a single job parameter
            var summary = new
            {
                tier = syncTier ?? "All",
                tableCount = entries.Count,
                succeeded = entries.Count(e => e.Outcome == SyncStatus.Outcome.Succeeded || e.Outcome == SyncStatus.Outcome.NoChanges),
                failed = entries.Count(e => e.Outcome == SyncStatus.Outcome.Failed),
                skipped = entries.Count(e => e.Outcome is SyncStatus.Outcome.SkippedLocked or SyncStatus.Outcome.SkippedDependency or "skipped_disabled"),
                totalRowsRead = entries.Sum(e => e.RowsRead),
                totalRowsUpserted = entries.Sum(e => e.RowsUpserted),
                totalRowsDeactivated = entries.Sum(e => e.RowsDeactivated),
                totalRowsDeleted = entries.Sum(e => e.RowsDeleted)
            };

            var connection = performContext.Connection;
            var jobId = performContext.BackgroundJob.Id;

            connection.SetJobParameter(jobId, "SyncTier", syncTier ?? "All");
            connection.SetJobParameter(jobId, "SyncSummary", JsonSerializer.Serialize(summary, jsonOptions));

            // Store per-table entries as individual parameters for detailed inspection
            foreach (var entry in entries)
            {
                var key = $"sync:{entry.SourceTable}";

                // Embed raw RowDetailsJson without double-encoding
                var rowDetails = entry.RowDetailsJson is not null
                    ? JsonDocument.Parse(entry.RowDetailsJson).RootElement
                    : (JsonElement?)null;

                var value = JsonSerializer.Serialize(new
                {
                    mode = entry.Mode,
                    outcome = entry.Outcome,
                    rowsRead = entry.RowsRead,
                    rowsUpserted = entry.RowsUpserted,
                    rowsDeactivated = entry.RowsDeactivated,
                    rowsDeleted = entry.RowsDeleted,
                    checkpointBefore = entry.CheckpointBefore,
                    checkpointAfter = entry.CheckpointAfter,
                    errorCode = entry.ErrorCode,
                    errorMessage = entry.ErrorMessage,
                    rowDetails
                }, jsonOptions);

                connection.SetJobParameter(jobId, key, value);
            }
        }
        catch (Exception ex)
        {
            // Never let parameter storage failures crash the job.
            // Hangfire will still show the default details (method, args, state history).
            System.Diagnostics.Debug.WriteLine(
                $"[WARN] Failed to store Hangfire job parameters: {ex.Message}");
        }
    }

    private static void SetSingleJobParameter(PerformContext performContext, string key, object value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            performContext.Connection.SetJobParameter(performContext.BackgroundJob.Id, key, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[WARN] Failed to store Hangfire job parameter '{key}': {ex.Message}");
        }
    }

    private static bool BelongsToTier(TableMappingRule rule, string? syncTier)
        => syncTier is null
            || string.Equals(rule.SyncTier, syncTier, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Hangfire job entry point for the original direct bootstrap path.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunBootstrapAsync(string sourceTable, Guid requestId)
    {
        using var scope = _scopeFactory.CreateScope();
        var requestStore = scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>();
        var request = await requestStore.GetAsync(requestId, CancellationToken.None);
        var running = request is not null && await requestStore.TryMarkRunningAsync(requestId,
            request.Status, request.HangfireJobId ?? string.Empty, CancellationToken.None);
        if (!running)
        {
            _logger.LogWarning(
                "Bootstrap request {RequestId} for {SourceTable} could not be claimed as Running",
                requestId, sourceTable);
            return;
        }

        await ExecuteInMemoryBootstrapAsync(scope.ServiceProvider, sourceTable, requestId);
    }

    /// <summary>
    /// Fenced claimed-job entry point. It is the sole owner-finalization path for
    /// pending-enqueue and terminal/missing-job recovery scheduling.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunClaimedBootstrapAsync(
        PerformContext performContext,
        string sourceTable,
        Guid requestId,
        string claimToken,
        bool isRecovery)
    {
        using var scope = _scopeFactory.CreateScope();
        var requestStore = scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>();
        var request = await requestStore.GetAsync(requestId, CancellationToken.None);
        if (request is null)
            return;

        if (!string.Equals(request.ReconcileClaimToken, claimToken, StringComparison.Ordinal))
            return;

        var expectation = new BootstrapRecoveryExpectation(
            request.RequestId,
            request.Status,
            request.HangfireJobId ?? string.Empty,
            request.ReconcileAttemptCount);
        var finalized = await requestStore.TryFinalizeClaimAsync(
            expectation,
            claimToken,
            performContext.BackgroundJob.Id,
            isRecovery,
            CancellationToken.None);
        if (!finalized)
            return;

        var actualJobId = performContext.BackgroundJob.Id;

        if (string.Equals(request.BootstrapType, BootstrapRequestType.Scalable,
                StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteScalableClaimedAsync(
                performContext, scope.ServiceProvider, sourceTable, requestId, actualJobId);
            return;
        }

        var running = await requestStore.TryMarkRunningAsync(requestId,
            BootstrapRequestStatus.Queued, actualJobId, CancellationToken.None);
        if (!running)
            return;

        await ExecuteInMemoryBootstrapAsync(scope.ServiceProvider, sourceTable, requestId,
            actualJobId);
    }

    private async Task ExecuteScalableClaimedAsync(
        PerformContext performContext,
        IServiceProvider serviceProvider,
        string ruleName,
        Guid requestId,
        string actualJobId)
    {
        var parentStore = serviceProvider.GetRequiredService<IBootstrapParentStore>();
        var requestStore = serviceProvider.GetRequiredService<IBootstrapRequestStore>();
        var parent = await parentStore.GetByRuleNameAsync(ruleName, CancellationToken.None);
        if (parent is null || parent.BootstrapRequestId != requestId)
        {
            await requestStore.TryFailAsync(requestId, BootstrapRequestStatus.Queued,
                actualJobId, "ScalableBootstrapParentMissing",
                $"No matching scalable parent exists for request {requestId} and rule '{ruleName}'.",
                CancellationToken.None);
            return;
        }

        if (!await requestStore.TryMarkRunningAsync(requestId, BootstrapRequestStatus.Queued,
                actualJobId, CancellationToken.None))
            return;
        switch (BootstrapParentRecoveryClassifier.Classify(parent))
        {
            case BootstrapParentRecoveryAction.StartPending:
                await RunFencedParentStartAsync(performContext, ruleName, parent.ParentId,
                    parent.FencingToken, performContext.BackgroundJob.Id);
                return;
            case BootstrapParentRecoveryAction.ResumeRunning:
                await ResumeScalableRunningPhaseAsync(serviceProvider, ruleName, parent);
                return;
            case BootstrapParentRecoveryAction.ResumeCatchingUp:
            case BootstrapParentRecoveryAction.ResumePublishing:
                await ResumeScalableFinalizePhaseAsync(serviceProvider, ruleName, parent);
                return;
            case BootstrapParentRecoveryAction.RecoveryPending:
                _logger.LogInformation("Parent {ParentId} is already recovery-pending", parent.ParentId);
                return;
            case BootstrapParentRecoveryAction.SyncCompleted:
                await requestStore.TryCompleteAsync(requestId, BootstrapRequestStatus.Running,
                    actualJobId, CancellationToken.None);
                return;
            case BootstrapParentRecoveryAction.SyncFailed:
                await requestStore.TryFailAsync(requestId, BootstrapRequestStatus.Running,
                    actualJobId,
                    parent.ErrorCode ?? "ScalableBootstrapParentFailed",
                    parent.ErrorMessage ?? $"Parent {parent.ParentId} is {parent.Status}.",
                    CancellationToken.None);
                return;
            default:
                await requestStore.TryFailAsync(requestId, BootstrapRequestStatus.Running,
                    actualJobId, "ScalableBootstrapParentStateUnknown",
                    $"Parent {parent.ParentId} has unsupported status '{parent.Status}'.", CancellationToken.None);
                return;
        }
    }

    private async Task ResumeScalableRunningPhaseAsync(
        IServiceProvider serviceProvider, string ruleName, BootstrapParent parent)
    {
        var childStore = serviceProvider.GetRequiredService<IBootstrapChildStore>();
        var scheduler = serviceProvider.GetRequiredService<IBootstrapJobScheduler>();
        var checker = serviceProvider.GetRequiredService<IBootstrapJobStateChecker>();
        var children = await childStore.GetByParentAsync(parent.ParentId, CancellationToken.None);

        var failed = children.LastOrDefault(c => c.Status == BootstrapChildStatus.Failed);
        if (failed is not null)
        {
            await serviceProvider.GetRequiredService<BootstrapFailureService>().FailAsync(parent,
                new BootstrapChildFailureExpectation(failed.ChildId, failed.ParentId,
                    failed.Status, failed.HangfireJobId),
                failed.ErrorCode ?? "ChildFailed", failed.ErrorMessage ?? "A child failed.", CancellationToken.None);
            return;
        }

        // Find active child for recovery inspection.
        var child = children.LastOrDefault(c => c.Status is BootstrapChildStatus.PendingEnqueue
            or BootstrapChildStatus.Queued or BootstrapChildStatus.Running);
        if (child is not null)
        {
            await RecoverChildIfTerminalAsync(serviceProvider, ruleName, parent, child, checker, childStore, scheduler);
            return;
        }

        // No active child — evaluate the completed chain for lost-finalize.
        await RecoverLostFinalizeOrNextChildAsync(serviceProvider, ruleName, parent, children, scheduler);
    }

    private async Task RecoverChildIfTerminalAsync(
        IServiceProvider serviceProvider, string ruleName, BootstrapParent parent, BootstrapChild child,
        IBootstrapJobStateChecker checker, IBootstrapChildStore childStore, IBootstrapJobScheduler scheduler)
    {
        var state = checker.Probe(child.HangfireJobId);
        if (state.Kind is BootstrapJobStateKind.Alive or BootstrapJobStateKind.Unknown)
            return;
        if (child.Status == BootstrapChildStatus.Running && state.Kind != BootstrapJobStateKind.Missing
            && state.Kind != BootstrapJobStateKind.TerminalFailure)
            return;

        var staleBefore = DateTime.UtcNow - _reconciliationPolicy.IdleAfter;
        var token = Guid.NewGuid().ToString("N");
        var expectation = new BootstrapChildRecoveryExpectation(child.ChildId, parent.ParentId,
            parent.FencingToken, child.Status, child.HangfireJobId, token, staleBefore);
        if (!await childStore.TryClaimRecoveryAsync(expectation, CancellationToken.None))
            return;
        await scheduler.ScheduleClaimedChildAsync(ruleName, parent.ParentId, child.ChildId,
            parent.FencingToken, child.Status, token, CancellationToken.None);
    }

    private async Task RecoverLostFinalizeOrNextChildAsync(
        IServiceProvider serviceProvider, string ruleName, BootstrapParent parent,
        IReadOnlyList<BootstrapChild> children, IBootstrapJobScheduler scheduler)
    {
        var requestStore = serviceProvider.GetRequiredService<IBootstrapRequestStore>();
        var checker = serviceProvider.GetRequiredService<IBootstrapJobStateChecker>();
        var childStore = serviceProvider.GetRequiredService<IBootstrapChildStore>();

        var latestChild = children.LastOrDefault();
        if (latestChild is null || latestChild.Status != BootstrapChildStatus.Completed)
            return;

        var isEof = IsEofChild(latestChild, children);
        if (isEof)
        {
            // All children completed, finalize was never scheduled or job is terminal/missing.
            var phaseObserved = checker.Probe(parent.PhaseJobId);
            if (phaseObserved.Kind is BootstrapJobStateKind.Alive or BootstrapJobStateKind.Unknown)
            {
                _logger.LogDebug("Phase job {PhaseJobId} for parent {ParentId} is alive/unknown — no-op",
                    parent.PhaseJobId, parent.ParentId);
                return;
            }

            var staleBefore = DateTime.UtcNow - _reconciliationPolicy.IdleAfter;
            var token = Guid.NewGuid().ToString("N");
            if (!await serviceProvider.GetRequiredService<IBootstrapParentStore>()
                    .TryClaimPhaseJobAsync(parent.ParentId, parent.FencingToken,
                        BootstrapParentStatus.Running, parent.PhaseJobId, token, staleBefore, CancellationToken.None))
                return;
            await scheduler.ScheduleClaimedFinalizeAsync(ruleName, parent.ParentId, parent.FencingToken,
                BootstrapParentStatus.Running, token, CancellationToken.None);
        }
        else
        {
            var nextChild = await childStore.TryCreateNextChildAsync(
                parent.ParentId, parent.FencingToken,
                latestChild.Sequence, latestChild.LastKey,
                latestChild.LastKey, CancellationToken.None);
            if (!nextChild.WasCreated)
                return;
            var claimToken = Guid.NewGuid().ToString("N");
            if (!await childStore.TryClaimInitialAsync(nextChild.Child.ChildId, parent.ParentId,
                    parent.FencingToken, claimToken, CancellationToken.None))
                return;
            await scheduler.ScheduleClaimedChildAsync(ruleName, parent.ParentId,
                nextChild.Child.ChildId, parent.FencingToken, BootstrapChildStatus.PendingEnqueue,
                claimToken, CancellationToken.None);
        }
    }

    private static bool IsEofChild(BootstrapChild latestChild, IReadOnlyList<BootstrapChild> allChildren)
    {
        // EOF is determined by rows_read < batchSize for the last completed child.
        // This invariant is already central to the child service.
        const int batchSize = 10_000;
        return latestChild.RowsRead < batchSize;
    }

    private async Task ResumeScalableFinalizePhaseAsync(
        IServiceProvider serviceProvider, string ruleName, BootstrapParent parent)
    {
        // If the parent already has a fresh phase claim (set atomically by reconciliation
        // via TryClaimScalableRecoveryAsync), skip the probe and use that token directly.
        if (parent.PhaseClaimToken is not null
            && parent.PhaseClaimedAt > DateTime.UtcNow - _reconciliationPolicy.IdleAfter)
        {
            await serviceProvider.GetRequiredService<IBootstrapJobScheduler>()
                .ScheduleClaimedFinalizeAsync(ruleName, parent.ParentId, parent.FencingToken,
                    parent.Status, parent.PhaseClaimToken, CancellationToken.None);
            return;
        }

        var checker = serviceProvider.GetRequiredService<IBootstrapJobStateChecker>();
        var phaseObserved = checker.Probe(parent.PhaseJobId);
        switch (phaseObserved.Kind)
        {
            case BootstrapJobStateKind.Alive:
            case BootstrapJobStateKind.Unknown:
                _logger.LogDebug("Phase job {PhaseJobId} for parent {ParentId} is alive/unknown — no-op",
                    parent.PhaseJobId, parent.ParentId);
                return;
            case BootstrapJobStateKind.TerminalSuccess:
                _logger.LogWarning("Phase job {PhaseJobId} for parent {ParentId} succeeded while parent is {Status}",
                    parent.PhaseJobId, parent.ParentId, parent.Status);
                return;
        }

        var staleBefore = DateTime.UtcNow - _reconciliationPolicy.IdleAfter;
        var token = Guid.NewGuid().ToString("N");
        var parentStore = serviceProvider.GetRequiredService<IBootstrapParentStore>();
        if (!await parentStore.TryClaimPhaseJobAsync(parent.ParentId, parent.FencingToken,
                parent.Status, parent.PhaseJobId, token, staleBefore, CancellationToken.None))
            return;
        await serviceProvider.GetRequiredService<IBootstrapJobScheduler>()
            .ScheduleClaimedFinalizeAsync(ruleName, parent.ParentId, parent.FencingToken,
                parent.Status, token, CancellationToken.None);
    }

    private async Task ExecuteInMemoryBootstrapAsync(
        IServiceProvider serviceProvider,
        string sourceTable,
        Guid requestId,
        string? expectedJobId = null)
    {
        var bootstrapService = serviceProvider.GetRequiredService<BootstrapSyncService>();
        var requestStore = serviceProvider.GetRequiredService<IBootstrapRequestStore>();
        var ownedRequest = await requestStore.GetAsync(requestId, CancellationToken.None);
        var jobSnapshot = expectedJobId ?? ownedRequest?.HangfireJobId ?? string.Empty;
        var rule = _ruleProvider.Get(sourceTable);
        var config = rule.ToTableSyncConfig();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var result = await bootstrapService.ExecuteAsync(config, requestId, cts.Token);
            switch (result.Outcome)
            {
                case SyncStatus.Outcome.SkippedLocked:
                    if (!await requestStore.TryMarkWaitingForLockAsync(requestId,
                            BootstrapRequestStatus.Running, jobSnapshot,
                            "Per-table advisory lock not acquired", CancellationToken.None))
                        return;
                    var waiting = await requestStore.GetAsync(requestId, CancellationToken.None);
                    if (waiting is null)
                        return;
                    var retryToken = Guid.NewGuid().ToString("N");
                    if (!await requestStore.TryClaimSlotAsync(
                            new BootstrapRecoveryExpectation(waiting.RequestId, waiting.Status,
                                waiting.HangfireJobId ?? string.Empty, waiting.ReconcileAttemptCount),
                            retryToken, DateTime.UtcNow - _reconciliationPolicy.IdleAfter,
                            isRecovery: false, CancellationToken.None))
                        return;
                    await serviceProvider.GetRequiredService<IBootstrapJobScheduler>()
                        .ScheduleClaimedAsync(sourceTable, requestId, retryToken, false,
                            TimeSpan.FromMinutes(1), CancellationToken.None);
                    break;

                case SyncStatus.Outcome.Succeeded:
                    try
                    {
                        var configStore = serviceProvider.GetRequiredService<ISyncConfigStore>();
                        await configStore.SeedAsync(config, CancellationToken.None);
                        await requestStore.TryCompleteAsync(requestId, BootstrapRequestStatus.Running,
                            jobSnapshot, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Bootstrap sync for {SourceTable} succeeded but seed of table_sync_config failed",
                            sourceTable);
                        await requestStore.TryFailAsync(
                            requestId,
                            BootstrapRequestStatus.Running,
                            jobSnapshot,
                            "SeedConfigFailed",
                            $"Data sync succeeded but table_sync_config seed failed: " +
                            (BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "configuration seed failed"),
                            CancellationToken.None);
                    }
                    break;

                default:
                    await requestStore.TryFailAsync(
                        requestId,
                        BootstrapRequestStatus.Running,
                        jobSnapshot,
                        result.ErrorCode ?? "BootstrapFailed",
                        result.ErrorMessage ?? "Bootstrap execution did not succeed",
                        CancellationToken.None);
                    break;
            }
        }
        catch (Exception ex)
        {
            await requestStore.TryFailAsync(
                requestId,
                BootstrapRequestStatus.Running,
                jobSnapshot,
                "BootstrapJobFailed",
                BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Bootstrap job failed.",
                CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// One-shot watchdog for a single bootstrap request. Runs only when scheduled
    /// at submit time; no-ops unless the request is still pending_enqueue.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task ReconcileBootstrapRequestAsync(string sourceTable, Guid requestId)
    {
        using var scope = _scopeFactory.CreateScope();
        var requestService = scope.ServiceProvider.GetRequiredService<BootstrapRequestService>();

        var context = new BootstrapReconciliationContext(
            BootstrapReconciliationMode.OneShot, DateTime.UtcNow);
        await requestService.ReconcileOneAsync(sourceTable, requestId, context, CancellationToken.None);
    }

    /// <summary>
    /// Optional batch reconcile retained for ops/manual recovery.
    /// Prefer the per-request watchdog path registered at submit time.
    /// </summary>
    public async Task ReconcilePendingBootstrapRequestsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var requestService = scope.ServiceProvider.GetRequiredService<BootstrapRequestService>();

        await requestService.ReconcileStaleAsync(DateTime.UtcNow, CancellationToken.None);
    }

    /// <summary>
    /// Scalable bootstrap: coordinator start (C0 capture, CREATE TABLE, child 1 enqueue).
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunParentStartAsync(PerformContext performContext, string ruleName, Guid parentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var parent = await scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>()
            .GetAsync(parentId, CancellationToken.None);
        if (parent is null || parent.Status != BootstrapParentStatus.PendingEnqueue)
            return;
        await RunFencedParentStartAsync(performContext, ruleName, parentId, parent.FencingToken,
            performContext.BackgroundJob.Id);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunFencedParentStartAsync(
        PerformContext performContext, string ruleName, Guid parentId, Guid fencingToken, string expectedJobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var parentStore = scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();
        var parent = await parentStore.GetAsync(parentId, CancellationToken.None);
        if (parent is null || parent.FencingToken != fencingToken
            || parent.Status != BootstrapParentStatus.PendingEnqueue)
            return;

        var executingJobId = performContext.BackgroundJob.Id;
        var ownedJobId = string.IsNullOrWhiteSpace(expectedJobId) ? executingJobId : expectedJobId;
        BootstrapRequest? linkedRequest = null;
        if (parent.BootstrapRequestId.HasValue)
        {
            linkedRequest = await scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>()
                .GetAsync(parent.BootstrapRequestId.Value, CancellationToken.None);
            if (linkedRequest is null || linkedRequest.HangfireJobId != executingJobId || linkedRequest.HangfireJobId != ownedJobId)
                return;
        }

        // Throttle: acquire DDL semaphore before starting a new parent.
        if (!_concurrencyManager.TryAcquireStageDdl())
        {
            string replacementJobId;
            try
            {
                replacementJobId = _backgroundJobClient.Schedule<CentralDbSyncJobs>(
                    job => job.RunFencedParentStartAsync(null!, ruleName, parentId, fencingToken, null!),
                    TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                if (linkedRequest is not null)
                {
                    var evidence = BootstrapDiagnosticSanitizer.Sanitize(
                        $"Parent-start replacement scheduling failed: {ex.GetType().Name}: {ex.Message}")
                        ?? "Parent-start replacement scheduling failed.";
                    await scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>()
                        .TryRecordScalableStartSchedulingFailureAsync(
                            new BootstrapRecoveryExpectation(linkedRequest.RequestId,
                                linkedRequest.Status, executingJobId,
                                linkedRequest.ReconcileAttemptCount),
                            parentId, fencingToken, BootstrapParentStatus.PendingEnqueue,
                            parent.PhaseJobId, "BootstrapParentStartScheduleFailed", evidence,
                            CancellationToken.None);
                }
                _logger.LogError(ex, "Failed to schedule throttled parent start replacement for {ParentId}", parentId);
                return;
            }

            if (parent.BootstrapRequestId.HasValue)
            {
                var reassigned = await scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>()
                    .TryReassignScalableStartJobAsync(parent.BootstrapRequestId.Value, parentId,
                        fencingToken, executingJobId, parent.PhaseJobId, replacementJobId,
                        CancellationToken.None);
                if (reassigned)
                    _logger.LogInformation("Stage DDL limit reached — atomically reassigned parent start {ParentId} to {JobId}", parentId, replacementJobId);
            }
            return;
        }

        try
        {
            var parentClaimToken = parent.PhaseClaimToken ?? Guid.NewGuid().ToString("N");
            if (parent.PhaseClaimToken is null &&
                !await parentStore.TryClaimPhaseJobAsync(parentId, fencingToken,
                    BootstrapParentStatus.PendingEnqueue, parent.PhaseJobId, parentClaimToken,
                    DateTime.UtcNow, CancellationToken.None))
                return;
            if (!await parentStore.TryFinalizePhaseJobAsync(parentId, fencingToken,
                    BootstrapParentStatus.PendingEnqueue, parentClaimToken, executingJobId,
                    "parent_start", CancellationToken.None))
                return;

            var coordinator = scope.ServiceProvider
                .GetRequiredService<ScalableBootstrapCoordinator>();
            await coordinator.StartAsync(parentId, CancellationToken.None);

            using var eventScope = _scopeFactory.CreateScope();
            var eventStore = eventScope.ServiceProvider.GetRequiredService<IBootstrapDiagnosticEventStore>();
            await eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                parent.BootstrapRequestId ?? Guid.Empty, parentId, null,
                BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.ParentClaimed,
                BootstrapParentStatus.PendingEnqueue, BootstrapParentStatus.Running,
                null, fencingToken.ToString(), null, null,
                "parent_start", null, "system"), CancellationToken.None);

            SetSingleJobParameter(performContext, "sync:result", new
            {
                phase = "ParentStart",
                ruleName,
                parentId = parentId.ToString(),
                outcome = "succeeded"
            });
        }
        finally
        {
            _concurrencyManager.ReleaseStageDdl();
        }
    }

    /// <summary>
    /// Scalable bootstrap: child execution (keyset read, batch COPY, cursor progression).
    /// Runs on the dedicated bootstrap-child queue.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    [Queue("bootstrap-child")]
    public async Task RunClaimedChildBootstrapAsync(PerformContext performContext, string ruleName,
        Guid parentId, Guid childId, Guid fencingToken, string expectedStatus, string claimToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var childStore = scope.ServiceProvider.GetRequiredService<IBootstrapChildStore>();
        // A job queued before persistence is harmless: only the holder of this exact
        // durable token can finalize and reach child execution.
        var finalized = expectedStatus == BootstrapChildStatus.PendingEnqueue
            ? await childStore.TryFinalizeInitialClaimAsync(childId, parentId, fencingToken,
                claimToken, performContext.BackgroundJob.Id, CancellationToken.None)
            : await childStore.TryFinalizeRecoveryAsync(childId, parentId, fencingToken,
                expectedStatus, claimToken, performContext.BackgroundJob.Id, CancellationToken.None);
        if (!finalized)
            return;
        await RunChildBootstrapAsync(performContext, ruleName, parentId, childId);

        using var childEventScope = _scopeFactory.CreateScope();
        var childEventStore = childEventScope.ServiceProvider.GetRequiredService<IBootstrapDiagnosticEventStore>();
        var parentStore = childEventScope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();
        var parent = await parentStore.GetAsync(parentId, CancellationToken.None);
        var childRequestId = parent?.BootstrapRequestId ?? Guid.Empty;
        await childEventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
            childRequestId, parentId, childId,
            BootstrapDiagnosticEntityType.Child, BootstrapDiagnosticEventType.ChildCompleted,
            null, null,
            null, fencingToken.ToString(), null, null,
            "child_completed", null, "system"), CancellationToken.None);
    }

    [AutomaticRetry(Attempts = 0)]
    [Queue("bootstrap-child")]
    public async Task RunChildBootstrapAsync(PerformContext performContext, string ruleName, Guid parentId, Guid childId)
    {
        // Throttle: if the child concurrency limit is reached, claim replacement
        // and schedule a fenced child job — no raw unclaimed reschedule.
        if (!_concurrencyManager.TryAcquireChild())
        {
            _logger.LogInformation(
                "Child bootstrap concurrency limit reached — claiming replacement for {ChildId}",
                childId);

            using var scope = _scopeFactory.CreateScope();
            var childStore = scope.ServiceProvider.GetRequiredService<IBootstrapChildStore>();
            var child = await childStore.GetAsync(childId, CancellationToken.None);
            if (child is null || child.ParentId != parentId)
                return;

            var parentStore = scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();
            var parent = await parentStore.GetAsync(parentId, CancellationToken.None);
            if (parent is null || parent.Status != BootstrapParentStatus.Running)
                return;

            var executingJobId = performContext.BackgroundJob.Id;
            var staleBefore = DateTime.UtcNow - _reconciliationPolicy.IdleAfter;
            var token = Guid.NewGuid().ToString("N");
            var expectation = new BootstrapChildRecoveryExpectation(childId, parentId,
                parent.FencingToken, child.Status, executingJobId, token, staleBefore);
            if (!await childStore.TryClaimRecoveryAsync(expectation, CancellationToken.None))
                return;

            try
            {
                _backgroundJobClient.Schedule<CentralDbSyncJobs>(
                    job => job.RunClaimedChildBootstrapAsync(null!, ruleName, parentId, childId,
                        parent.FencingToken, child.Status, token),
                    TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                var evidence = BootstrapDiagnosticSanitizer.Sanitize(
                    $"Claimed child replacement scheduling failed: {ex.GetType().Name}: {ex.Message}")
                    ?? "Claimed child replacement scheduling failed.";
                _logger.LogError(ex,
                    "Failed to schedule claimed child replacement for {ChildId} — claim persists for stale takeover",
                    childId);
                await childStore.TryRecordRecoverySchedulingFailureAsync(expectation,
                    "BootstrapChildRecoveryScheduleFailed", evidence, CancellationToken.None);
                var requestStore = scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>();
                var request = parent.BootstrapRequestId.HasValue
                    ? await requestStore.GetAsync(parent.BootstrapRequestId.Value, CancellationToken.None)
                    : null;
                if (request is not null)
                    await requestStore.TryFailScalableChildSchedulingExhaustedAsync(
                        new BootstrapRecoveryExpectation(request.RequestId, request.Status,
                            request.HangfireJobId ?? string.Empty, request.ReconcileAttemptCount),
                        new BootstrapChildFailureExpectation(child.ChildId, child.ParentId,
                            child.Status, child.HangfireJobId), parent.FencingToken, parent.Status,
                        parent.LastHeartbeatAt, parent.PhaseJobId,
                        "BootstrapRecoverySchedulingExhausted", evidence, CancellationToken.None);
            }
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var childService = scope.ServiceProvider.GetRequiredService<BootstrapChildService>();
            var parentStore = scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();

            var parent = await parentStore.GetAsync(parentId, CancellationToken.None);
            if (parent is null)
            {
                _logger.LogWarning("Parent {ParentId} not found for child {ChildId}", parentId, childId);
                SetSingleJobParameter(performContext, "sync:result", new
                {
                    phase = "ChildBootstrap",
                    ruleName,
                    parentId = parentId.ToString(),
                    childId = childId.ToString(),
                    outcome = "skipped_parent_not_found"
                });
                return;
            }

            var result = await childService.RunAsync(
                childId, parentId, parent.FencingToken,
                parent.StagingSchema, parent.StagingTableName,
                CancellationToken.None);

            if (!result.IsSuccess)
            {
                SetSingleJobParameter(performContext, "sync:result", new
                {
                    phase = "ChildBootstrap",
                    ruleName,
                    parentId = parentId.ToString(),
                    childId = childId.ToString(),
                    outcome = "failed",
                    errorCode = result.ErrorCode,
                    errorMessage = result.ErrorMessage
                });

                throw new InvalidOperationException(
                    $"Child bootstrap {childId} for rule {ruleName} failed: " +
                    $"{result.ErrorCode} — {result.ErrorMessage}");
            }

            SetSingleJobParameter(performContext, "sync:result", new
            {
                phase = "ChildBootstrap",
                ruleName,
                parentId = parentId.ToString(),
                childId = childId.ToString(),
                outcome = "succeeded",
                rowsRead = result.RowsRead,
                isEof = result.IsEof,
                lastKey = result.LastKey
            });
        }
        finally
        {
            _concurrencyManager.ReleaseChild();
        }
    }

    /// <summary>
    /// Scalable bootstrap: coordinator finalize (CT catch-up + atomic publish + DROP TABLE).
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunClaimedCoordinatorFinalizeAsync(PerformContext performContext, string ruleName,
        Guid parentId, Guid fencingToken, string expectedStatus, string claimToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var parentStore = scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();
        if (!await parentStore.TryFinalizePhaseJobAsync(parentId, fencingToken, expectedStatus,
                claimToken, performContext.BackgroundJob.Id, expectedStatus, CancellationToken.None))
            return;
        await RunCoordinatorFinalizeAsync(performContext, ruleName, parentId);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunClaimedScalableRecoveryCoordinatorFinalizeAsync(PerformContext performContext,
        string ruleName, Guid requestId, Guid parentId, Guid fencingToken, string expectedStatus,
        string? expectedPhaseJobId, string claimToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var requestStore = scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>();
        var request = await requestStore.GetAsync(requestId, CancellationToken.None);
        if (request is null || !string.Equals(request.ReconcileClaimToken, claimToken, StringComparison.Ordinal))
            return;

        var requestExpectation = new BootstrapRecoveryExpectation(request.RequestId, request.Status,
            request.HangfireJobId ?? string.Empty, request.ReconcileAttemptCount);
        var parentExpectation = new BootstrapParentPhaseJobExpectation(parentId, fencingToken,
            expectedStatus, expectedPhaseJobId, claimToken, DateTime.MinValue);
        if (!await requestStore.TryFinalizeScalableRecoveryClaimAsync(requestExpectation,
                parentExpectation, claimToken, performContext.BackgroundJob.Id, CancellationToken.None))
            return;

        await RunCoordinatorFinalizeAsync(performContext, ruleName, parentId);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunCoordinatorFinalizeAsync(PerformContext performContext, string ruleName, Guid parentId)
    {
        // Throttle: acquire DDL semaphore before finalize.
        // Covers CT catch-up + final publish transaction (which includes DROP TABLE).
        if (!_concurrencyManager.TryAcquireStageDdl())
        {
            _logger.LogInformation(
                "Stage DDL concurrency limit reached — claiming replacement for {RuleName}",
                ruleName);

            using var scope = _scopeFactory.CreateScope();
            var parentStore = scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();
            var parent = await parentStore.GetByRuleNameAsync(ruleName, CancellationToken.None);
            if (parent is null || parent.Status is not
                    (BootstrapParentStatus.Running or BootstrapParentStatus.CatchingUp or BootstrapParentStatus.Publishing))
                return;

            var executingJobId = performContext.BackgroundJob.Id;
            var staleBefore = DateTime.UtcNow - _reconciliationPolicy.IdleAfter;
            var token = Guid.NewGuid().ToString("N");
            if (!await parentStore.TryClaimPhaseJobAsync(parent.ParentId, parent.FencingToken,
                    parent.Status, executingJobId, token, staleBefore, CancellationToken.None))
                return;

            try
            {
                _backgroundJobClient.Schedule<CentralDbSyncJobs>(
                    job => job.RunClaimedCoordinatorFinalizeAsync(null!, ruleName, parentId,
                        parent.FencingToken, parent.Status, token),
                    TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                var evidence = BootstrapDiagnosticSanitizer.Sanitize(
                    $"Claimed finalize replacement scheduling failed: {ex.GetType().Name}: {ex.Message}")
                    ?? "Claimed finalize replacement scheduling failed.";
                _logger.LogError(ex,
                    "Failed to schedule claimed finalize replacement for {RuleName} — claim persists for stale takeover",
                    ruleName);
                var requestStore = scope.ServiceProvider.GetRequiredService<IBootstrapRequestStore>();
                var request = parent.BootstrapRequestId.HasValue
                    ? await requestStore.GetAsync(parent.BootstrapRequestId.Value, CancellationToken.None)
                    : null;
                if (request is not null)
                {
                    await requestStore.TryRecordScalablePhaseSchedulingFailureAsync(
                        new BootstrapRecoveryExpectation(request.RequestId, request.Status,
                            request.HangfireJobId ?? string.Empty, request.ReconcileAttemptCount),
                        new BootstrapParentPhaseJobExpectation(parent.ParentId, parent.FencingToken,
                            parent.Status, parent.PhaseJobId, token, DateTime.MinValue),
                        "BootstrapPhaseRecoveryScheduleFailed", evidence, CancellationToken.None);
                }
            }
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<ScalableBootstrapCoordinator>();
            await coordinator.FinalizeAsync(parentId, CancellationToken.None);
        }
        finally
        {
            _concurrencyManager.ReleaseStageDdl();
        }
    }

    /// <summary>
    /// CT continuation after final publish. Uses the standard sync orchestrator
    /// to catch up changes past C1 for the bootstrapped rule. Since the checkpoint
    /// was advanced to C1 with status 'Ready' during publish, the orchestrator
    /// routes to the CT incremental sync path.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunBootstrapCtContinuationAsync(string ruleName, Guid parentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var ruleProvider = scope.ServiceProvider.GetRequiredService<IMappingRuleProvider>();
        var configStore = scope.ServiceProvider.GetRequiredService<ISyncConfigStore>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SyncOrchestrator>();

        var rule = ruleProvider.Get(ruleName);
        if (rule is null || !rule.Enabled)
        {
            _logger.LogWarning(
                "CT continuation: rule {RuleName} not found or disabled", ruleName);
            return;
        }

        var config = rule.ToTableSyncConfig();
        if (!await configStore.IsEnabledAsync(config.SourceTable, CancellationToken.None))
        {
            _logger.LogDebug(
                "CT continuation: {SourceTable} disabled at runtime", config.SourceTable);
            return;
        }

        _logger.LogInformation(
            "CT continuation for {RuleName} (parent {ParentId}): running orchestrator from C1 checkpoint",
            ruleName, parentId);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        await orchestrator.ExecuteAsync(new[] { config }, timeoutCts.Token);
    }

    /// <summary>
    /// CT dispatch reconciliation: processes pending dispatch markers by claiming
    /// them with a lease, enqueuing the CT continuation job, and marking them as
    /// dispatched. Runs frequently via recurring job.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 30)]
    public async Task RunCtDispatchReconciliationAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dispatchService = scope.ServiceProvider
            .GetRequiredService<IBootstrapCtDispatchService>();
        await dispatchService.DispatchPendingAsync(ct);
    }

    /// <summary>
    /// Orphan stage cleanup: drops dynamic staging tables for failed/stale/expired parents.
    /// Runs daily via recurring job.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunOrphanStageCleanupAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var parentStore = scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();
        var stagingStore = scope.ServiceProvider.GetRequiredService<ITypedBootstrapStagingStore>();
        var options = scope.ServiceProvider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<Infrastructure.CentralDbSync.CentralDbSyncOptions>>();
        var retention = options.Value.BootstrapOrphanStageRetention;
        var cutoff = DateTime.UtcNow.Add(-retention);

        var candidates = await parentStore.GetCleanupCandidatesAsync(cutoff, CancellationToken.None);
        var dropped = 0;
        var needsRetry = false;

        foreach (var parent in candidates)
        {
            try
            {
                // Acquire DDL slot BEFORE modifying parent status.
                // If the DDL concurrency limit is reached, do NOT expire the parent —
                // just skip it and schedule a retry. Without this reordering, a parent
                // could be stuck Expired-with-table for 24h until the next recurring run.
                if (!_concurrencyManager.TryAcquireStageDdl())
                {
                    _logger.LogInformation(
                        "Stage DDL concurrency limit reached — deferring cleanup of {Schema}.{Table} for parent {ParentId}",
                        parent.StagingSchema, parent.StagingTableName, parent.ParentId);
                    needsRetry = true;
                    continue;
                }

                try
                {
                    if (parent.Status == BootstrapParentStatus.CancelRequested)
                    {
                        await using var cancelConn = _connectionFactory.CreateConnection();
                        await cancelConn.OpenAsync(CancellationToken.None);
                        await using var cancelTx = await cancelConn.BeginTransactionAsync(CancellationToken.None);

                        await stagingStore.DropStageAsync(
                            cancelConn, cancelTx, parent.StagingSchema, parent.StagingTableName, CancellationToken.None);

                        await cancelTx.CommitAsync(CancellationToken.None);
                        await parentStore.TryMarkCancelledAsync(parent.ParentId, CancellationToken.None);

                        dropped++;

                        _logger.LogInformation(
                            "Dropped orphan stage {Schema}.{Table} for cancelled parent {ParentId}",
                            parent.StagingSchema, parent.StagingTableName, parent.ParentId);

                        continue;
                    }

                    // Claim parent via CAS (transition to Expired)
                    var claimed = await parentStore.TryTransitionAsync(
                        parent.ParentId, parent.FencingToken,
                        parent.Status, BootstrapParentStatus.Expired, CancellationToken.None);

                    if (!claimed)
                    {
                        _logger.LogWarning(
                            "Orphan stage {Schema}.{Table} for parent {ParentId} could not be claimed — skipping",
                            parent.StagingSchema, parent.StagingTableName, parent.ParentId);
                        continue;
                    }

                    await using var conn = _connectionFactory.CreateConnection();
                    await conn.OpenAsync(CancellationToken.None);
                    await using var tx = await conn.BeginTransactionAsync(CancellationToken.None);

                    await stagingStore.DropStageAsync(
                        conn, tx, parent.StagingSchema, parent.StagingTableName, CancellationToken.None);

                    await tx.CommitAsync(CancellationToken.None);
                    await parentStore.SetCleanupCompletedAsync(parent.ParentId, CancellationToken.None);

                    dropped++;

                    _logger.LogInformation(
                        "Dropped orphan stage {Schema}.{Table} for parent {ParentId}",
                        parent.StagingSchema, parent.StagingTableName, parent.ParentId);
                }
                finally
                {
                    _concurrencyManager.ReleaseStageDdl();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to drop orphan stage {Schema}.{Table} for parent {ParentId}",
                    parent.StagingSchema, parent.StagingTableName, parent.ParentId);
            }
        }

        // Schedule a retry if any candidate was deferred due to DDL concurrency
        if (needsRetry)
        {
            _logger.LogInformation(
                "DDL slot was full for some cleanup candidates — scheduling retry in 1 minute");
            _backgroundJobClient.Schedule<CentralDbSyncJobs>(
                job => job.RunOrphanStageCleanupAsync(),
                TimeSpan.FromMinutes(1));
        }

        _logger.LogInformation(
            "Orphan stage cleanup: {Dropped}/{Total} tables dropped",
            dropped, candidates.Count);
    }

    /// <summary>
    /// Immediately cleans a parent after a cancellation request. The daily orphan
    /// cleanup remains the recovery path when this job cannot run or fails.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task RunCancelBootstrapCleanupAsync(Guid parentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var parentStore = scope.ServiceProvider.GetRequiredService<IBootstrapParentStore>();
        var parent = await parentStore.GetAsync(parentId, CancellationToken.None);

        if (parent is null || parent.Status != BootstrapParentStatus.CancelRequested)
            return;

        var stagingStore = scope.ServiceProvider.GetRequiredService<ITypedBootstrapStagingStore>();
        if (!_concurrencyManager.TryAcquireStageDdl())
            throw new InvalidOperationException("Stage DDL concurrency limit reached; cancellation cleanup will retry.");

        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(CancellationToken.None);
            await using var tx = await conn.BeginTransactionAsync(CancellationToken.None);

            if (parent.StagingCreatedAt.HasValue)
            {
                await stagingStore.DropStageAsync(
                    conn, tx, parent.StagingSchema, parent.StagingTableName, CancellationToken.None);
            }

            await tx.CommitAsync(CancellationToken.None);
            await parentStore.TryMarkCancelledAsync(parent.ParentId, CancellationToken.None);
        }
        finally
        {
            _concurrencyManager.ReleaseStageDdl();
        }
    }

    /// <summary>
    /// Prunes diagnostic events older than 90 days from <c>sync_meta.bootstrap_diagnostic_event</c>.
    /// Runs daily via recurring job.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task PruneBootstrapDiagnosticEventsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IBootstrapDiagnosticEventStore>();
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var deleted = await eventStore.DeleteBeforeAsync(cutoff, CancellationToken.None);
        _logger.LogInformation("Pruned {Count} bootstrap diagnostic events older than 90 days", deleted);
    }
}
