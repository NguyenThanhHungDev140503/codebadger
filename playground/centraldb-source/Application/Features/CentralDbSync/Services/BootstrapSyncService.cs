using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Application.Features.CentralDbSync.Validation;
using Microsoft.Extensions.Logging;

namespace Application.Features.CentralDbSync.Services;

public sealed class BootstrapSyncService(
    ITableSyncLock syncLock,
    IBootstrapSnapshotReader reader,
    ISyncBatchApplier applier,
    IMappingRuleProvider ruleProvider,
    ISyncRunLog runLog,
    ILogger<BootstrapSyncService> logger)
{
    public Task<SyncRunResult> ExecuteAsync(
        TableSyncConfig config,
        CancellationToken cancellationToken = default)
    {
        // Validation is done in the Guid-runId overload called below
        return ExecuteAsync(config, Guid.NewGuid(), cancellationToken);
    }

    public async Task<SyncRunResult> ExecuteAsync(
        TableSyncConfig config,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        SyncGuard.AssertValidConfig(config);

        // Route scalable bootstrap rules through the coordinator
        var rule = ruleProvider.Get(config.SourceTable);
        if (rule.UseScalableBootstrap)
        {
            throw new InvalidOperationException(
                $"Rule '{rule.RuleName}' uses scalable bootstrap. " +
                "Use ScalableBootstrapCoordinator instead of BootstrapSyncService.");
        }

        var startedAt = DateTime.UtcNow;

        await using var lockHandle = await syncLock.TryAcquireAsync(
            config.SourceTable, cancellationToken, TimeSpan.FromMinutes(12));

        if (lockHandle is null)
        {
            logger.LogDebug(
                "Bootstrap sync skipped for {SourceTable}: per-table lock not acquired",
                config.SourceTable);

            SyncGuard.AssertValidOutcome(
                SyncStatus.Outcome.SkippedLocked, nameof(SyncStatus.Outcome));
            await runLog.WriteAsync(new SyncRunLogEntry
            {
                RunId = runId,
                SourceTable = config.SourceTable,
                Mode = "Bootstrap",
                Outcome = SyncStatus.Outcome.SkippedLocked,
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow,
                DurationMs = 0
            }, CancellationToken.None);

            return new SyncRunResult { Outcome = SyncStatus.Outcome.SkippedLocked };
        }

        return await ExecuteCoreAsync(config, runId, startedAt, cancellationToken);
    }

    /// <summary>
    /// Runs bootstrap using an already-acquired lock. The caller owns the lock handle lifecycle.
    /// Used by ChangeTrackingSyncService for CT-invalid recovery to avoid releasing and
    /// re-acquiring the lock (which would allow another worker to steal it).
    /// The scalable CT recovery path routes through the coordinator instead.
    /// </summary>
    public async Task<SyncRunResult> ExecuteWithProvidedLockAsync(
        TableSyncConfig config,
        Guid runId,
        DateTime startedAt,
        CancellationToken cancellationToken = default)
    {
        SyncGuard.AssertValidConfig(config);

        // Scalable rules are recovered by ScalableBootstrapCoordinator through an explicit
        // bootstrap request. Report it as an outcome instead of throwing: the caller is the
        // CT recovery path, and an exception there would abort the whole sync run.
        var rule = ruleProvider.Get(config.SourceTable);
        if (rule.UseScalableBootstrap)
        {
            logger.LogWarning(
                "Bootstrap recovery for {RuleName} requires a scalable bootstrap request",
                rule.RuleName);

            return new SyncRunResult { Outcome = SyncStatus.Outcome.RequiresFullResync };
        }

        return await ExecuteCoreAsync(config, runId, startedAt, cancellationToken);
    }

    /// <summary>
    /// Core bootstrap logic (lock already acquired by the caller).
    /// </summary>
    /// <remarks>
    /// <b>Two-phase flow:</b>
    /// <list type="number">
    /// <item>
    /// <b>Read snapshot</b> —
    /// <see cref="IBootstrapSnapshotReader.ReadAsync"/> opens a SQL Server transaction,
    /// captures <c>CHANGE_TRACKING_CURRENT_VERSION()</c> as the baseline, SELECTs all
    /// rows from the source table, then re-reads the version to confirm consistency.
    /// If the version drifted (another writer committed mid-read), it retries up to
    /// 3 times. The result is a <see cref="BootstrapSnapshot"/> whose Rows are
    /// guaranteed to match BaselineVersion.
    /// </item>
    /// <item>
    /// <b>Apply to target</b> —
    /// <see cref="ISyncBatchApplier.ApplyBootstrapAsync"/> upserts every row into
    /// PostgreSQL, deactivates any target rows no longer present in the source
    /// (orphan cleanup), and atomically records the checkpoint as BaselineVersion.
    /// </item>
    /// </list>
    /// <para>
    /// On success the <see cref="SyncRunLogEntry"/> records <c>CheckpointAfter</c>
    /// = snapshot.BaselineVersion so that future incremental (ChangeTracking) sync
    /// runs can pick up from this version.
    /// </para>
    /// <para>
    /// On failure the checkpoint is <b>not</b> modified — only the run log and
    /// request status reflect the error. Operators can safely retry bootstrap from
    /// the same state.
    /// </para>
    /// </remarks>
    private async Task<SyncRunResult> ExecuteCoreAsync(
        TableSyncConfig config,
        Guid runId,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            // Capture a point-in-time snapshot: full row set + Change Tracking version
            // inside a single consistent transaction (with retry if version drifts).
            var snapshot = await reader.ReadAsync(config, cancellationToken);

            // Upsert all snapshot rows into PostgreSQL, deactivate orphans,
            // and atomically set checkpoint = snapshot.BaselineVersion.
            var result = await applier.ApplyBootstrapAsync(config, snapshot, cancellationToken);

            var finishedAt = DateTime.UtcNow;
            SyncGuard.AssertValidOutcome(result.Outcome, nameof(result.Outcome));
            await runLog.WriteAsync(new SyncRunLogEntry
            {
                RunId = runId,
                SourceTable = config.SourceTable,
                Mode = "Bootstrap",
                Outcome = result.Outcome,
                RowsRead = snapshot.Rows.Count,
                RowsUpserted = result.RowsUpserted,
                RowsDeactivated = result.RowsDeactivated,
                RowsDeleted = result.RowsDeleted,
                CheckpointBefore = null,
                CheckpointAfter = snapshot.BaselineVersion,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                DurationMs = (int)(finishedAt - startedAt).TotalMilliseconds
            }, cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Bootstrap sync cancelled for {SourceTable}", config.SourceTable);
            await runLog.WriteAsync(
                CreateFailedEntry(config.SourceTable, runId, startedAt, "Cancelled", "Operation was cancelled"),
                CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bootstrap sync failed for {SourceTable}", config.SourceTable);

            // Bootstrap errors must NOT modify the checkpoint — only the run log
            // and request status should reflect the failure. Operators can retry
            // from the same checkpoint.
            await runLog.WriteAsync(
                CreateFailedEntry(config.SourceTable, runId, startedAt, "BootstrapFailed",
                    BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Bootstrap failed."),
                CancellationToken.None);

            return new SyncRunResult
            {
                Outcome = SyncStatus.Outcome.Failed,
                ErrorCode = "BootstrapFailed",
                    ErrorMessage = BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Bootstrap failed."
            };
        }
    }

    private static SyncRunLogEntry CreateFailedEntry(
        string sourceTable, Guid runId, DateTime startedAt, string errorCode, string errorMessage)
    {
        var now = DateTime.UtcNow;
        return new SyncRunLogEntry
        {
            RunId = runId,
            SourceTable = sourceTable,
            Mode = "Bootstrap",
            Outcome = SyncStatus.Outcome.Failed,
            StartedAt = startedAt,
            FinishedAt = now,
            DurationMs = (int)(now - startedAt).TotalMilliseconds,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
