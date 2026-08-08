using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Application.Features.CentralDbSync.Validation;
using Microsoft.Extensions.Logging;

namespace Application.Features.CentralDbSync.Services;

public sealed class SyncOrchestrator(
    BootstrapSyncService bootstrapService,
    ChangeTrackingSyncService ctService,
    ISyncCheckpointStore checkpointStore,
    IMappingRuleProvider ruleProvider,
    ISyncRunLog runLog,
    ILogger<SyncOrchestrator> logger)
{
    /// <summary>
    /// Runs sync for all configured tables. Tables are processed in caller order;
    /// dependant tables are skipped when their upstream dependencies are not Ready.
    /// Failures in one table do not block independent tables from being processed.
    /// </summary>
    public async Task<IReadOnlyList<SyncRunLogEntry>> ExecuteAsync(
        TableSyncConfig[] configs,
        CancellationToken cancellationToken = default)
    {
        var allEntries = new List<SyncRunLogEntry>(configs.Length);

        foreach (var config in configs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            SyncGuard.AssertValidConfig(config);

            if (!config.Enabled)
            {
                logger.LogDebug("Table {SourceTable} is disabled, skipping", config.SourceTable);
                var skippedEntry = new SyncRunLogEntry
                {
                    SourceTable = config.SourceTable,
                    Mode = "Skipped",
                    Outcome = "skipped_disabled",
                    StartedAt = DateTime.UtcNow,
                    FinishedAt = DateTime.UtcNow,
                    DurationMs = 0
                };
                allEntries.Add(skippedEntry);
                continue;
            }

            // Check all upstream dependencies are Ready before processing this table
            if (!await AreDependenciesReadyAsync(config, cancellationToken))
            {
                logger.LogWarning(
                    "Table {SourceTable} skipped: one or more dependencies are not Ready",
                    config.SourceTable);

                SyncGuard.AssertValidOutcome(
                    SyncStatus.Outcome.SkippedDependency, nameof(SyncStatus.Outcome));
                var skippedDepEntry = new SyncRunLogEntry
                {
                    SourceTable = config.SourceTable,
                    Mode = "Orchestrator",
                    Outcome = SyncStatus.Outcome.SkippedDependency,
                    StartedAt = DateTime.UtcNow,
                    FinishedAt = DateTime.UtcNow,
                    DurationMs = 0
                };
                allEntries.Add(skippedDepEntry);
                await runLog.WriteAsync(skippedDepEntry, CancellationToken.None);

                continue;
            }

            // Determine which sync path to take based on current checkpoint state
            var checkpoint = await checkpointStore.GetAsync(config.SourceTable, cancellationToken);

            SyncRunResult result;
            if (checkpoint is null
                || checkpoint.SyncStatus == SyncStatus.CheckpointState.PendingInitialSync
                || checkpoint.SyncStatus == SyncStatus.CheckpointState.RequiresFullResync)
            {
                if (UsesScalableBootstrap(config.SourceTable))
                {
                    // Scalable rules bootstrap only through ScalableBootstrapCoordinator,
                    // triggered by an explicit bootstrap request. Calling BootstrapSyncService
                    // here would throw and abort the remaining tables in this run.
                    logger.LogDebug(
                        "Table {SourceTable} skipped: scalable bootstrap has not published a checkpoint yet",
                        config.SourceTable);
                    continue;
                }

                result = await bootstrapService.ExecuteAsync(config, cancellationToken);
            }
            else
            {
                result = await ctService.ExecuteAsync(config, cancellationToken);
            }

            var entry = new SyncRunLogEntry
            {
                SourceTable = config.SourceTable,
                Mode = checkpoint is null || checkpoint.SyncStatus != SyncStatus.CheckpointState.Ready
                    ? "Bootstrap"
                    : "ChangeTracking",
                Outcome = result.Outcome,
                RowsRead = result.RowsRead,
                RowsUpserted = result.RowsUpserted,
                RowsDeactivated = result.RowsDeactivated,
                RowsDeleted = result.RowsDeleted,
                CheckpointBefore = result.CheckpointBefore,
                CheckpointAfter = result.CheckpointAfter,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage,
                RowDetailsJson = result.RowDetailsJson,
                StartedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow
            };
            allEntries.Add(entry);

            logger.LogInformation(
                "Table {SourceTable} sync completed: outcome={Outcome}, "
                + "rowsRead={RowsRead}, rowsUpserted={RowsUpserted}, "
                + "rowsDeactivated={RowsDeactivated}",
                config.SourceTable,
                result.Outcome,
                result.RowsRead,
                result.RowsUpserted,
                result.RowsDeactivated);
        }

        return allEntries;
    }

    private bool UsesScalableBootstrap(string ruleName)
        => ruleProvider.TryGet(ruleName, out var rule) && rule.UseScalableBootstrap;

    private async Task<bool> AreDependenciesReadyAsync(
        TableSyncConfig config,
        CancellationToken cancellationToken)
    {
        if (config.Dependency is null || config.Dependency.Length == 0)
            return true;

        foreach (var dep in config.Dependency)
        {
            var depCheckpoint = await checkpointStore.GetAsync(dep, cancellationToken);
            if (depCheckpoint?.SyncStatus != SyncStatus.CheckpointState.Ready)
            {
                logger.LogWarning(
                    "Dependency {Dependency} for table {SourceTable} is not Ready (status: {Status})",
                    dep, config.SourceTable, depCheckpoint?.SyncStatus ?? "null");
                return false;
            }
        }

        return true;
    }
}
