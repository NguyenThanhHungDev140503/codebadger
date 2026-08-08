using Application.Features.CentralDbSync;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Application.Features.CentralDbSync.Validation;
using Microsoft.Extensions.Logging;

namespace Application.Features.CentralDbSync.Services;

public sealed class ChangeTrackingSyncService(
    ITableSyncLock syncLock,
    IChangeTrackingReader reader,
    ISyncBatchApplier applier,
    ISyncCheckpointStore checkpointStore,
    ISyncRunLog runLog,
    BootstrapSyncService bootstrapService,
    ILogger<ChangeTrackingSyncService> logger)
{
    private const int MaxApplyRetries = 3;

    public async Task<SyncRunResult> ExecuteAsync(
        TableSyncConfig config,
        CancellationToken cancellationToken = default)
    {
        SyncGuard.AssertValidConfig(config);

        var startedAt = DateTime.UtcNow;

        await using var lockHandle = await syncLock.TryAcquireAsync(
            config.SourceTable, cancellationToken, TimeSpan.FromMinutes(7));

        if (lockHandle is null)
        {
            logger.LogDebug(
                "CT sync skipped for {SourceTable}: per-table lock not acquired",
                config.SourceTable);

            await runLog.WriteAsync(new SyncRunLogEntry
            {
                SourceTable = config.SourceTable,
                Mode = "ChangeTracking",
                Outcome = SyncStatus.Outcome.SkippedLocked,
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow,
                DurationMs = 0
            }, CancellationToken.None);

            return new SyncRunResult { Outcome = SyncStatus.Outcome.SkippedLocked };
        }

        try
        {
            // Read current checkpoint — must be Ready with a valid LastSyncVersion
            var checkpoint = await checkpointStore.GetAsync(config.SourceTable, cancellationToken);

            if (checkpoint is null)
            {
                logger.LogInformation(
                    "CT sync skipped for {SourceTable}: no checkpoint exists → requires_full_resync",
                    config.SourceTable);
                return new SyncRunResult { Outcome = SyncStatus.Outcome.RequiresFullResync };
            }

            if (checkpoint.SyncStatus != SyncStatus.CheckpointState.Ready)
            {
                logger.LogInformation(
                    "CT sync skipped for {SourceTable}: checkpoint status is {Status} → requires_full_resync",
                    config.SourceTable, checkpoint.SyncStatus);
                return new SyncRunResult { Outcome = SyncStatus.Outcome.RequiresFullResync };
            }

            if (checkpoint.LastSyncVersion is null)
            {
                logger.LogInformation(
                    "CT sync skipped for {SourceTable}: checkpoint has no LastSyncVersion → requires_full_resync",
                    config.SourceTable);
                return new SyncRunResult { Outcome = SyncStatus.Outcome.RequiresFullResync };
            }

            ChangeBatch batch;
            try
            {
                // Read changes since the last checkpoint
                // IChangeTrackingReader captures UpperWatermark before enumerating CT changes
                batch = await reader.ReadBatchAsync(
                    config, checkpoint.LastSyncVersion.Value, cancellationToken);
            }
            catch (CheckpointInvalidException)
            {
                logger.LogWarning(
                    "Checkpoint invalid for {SourceTable}: transitioning and running immediate bootstrap recovery",
                    config.SourceTable);
                SyncGuard.AssertValidCheckpointStatus(
                    SyncStatus.CheckpointState.RequiresFullResync, nameof(SyncStatus.CheckpointState));
                await checkpointStore.TransitionToFullResyncAsync(
                    config.SourceTable, "CheckpointInvalid",
                    "CT checkpoint is below minimum valid version", cancellationToken);

                logger.LogInformation(
                    "Running immediate bootstrap recovery for {SourceTable}", config.SourceTable);

                // Bootstrap recovery runs under the same lock — never release and re-acquire,
                // as another worker could steal the lock between the two operations.
                return await bootstrapService.ExecuteWithProvidedLockAsync(
                    config, Guid.NewGuid(), startedAt, cancellationToken);
            }

            if (batch.Rows.Count == 0)
            {
                // Atomically advance checkpoint so the same empty window is not re-read
                var advanced = await checkpointStore.AdvanceAsync(
                    config.SourceTable,
                    checkpoint.LastSyncVersion.Value,
                    batch.UpperWatermark,
                    cancellationToken);

                if (!advanced)
                {
                    logger.LogWarning(
                        "No-change checkpoint advance lost race for {SourceTable}: another worker advanced already",
                        config.SourceTable);
                }

                var noChangesResult = new SyncRunResult
                {
                    Outcome = SyncStatus.Outcome.NoChanges,
                    CheckpointBefore = checkpoint.LastSyncVersion,
                    CheckpointAfter = batch.UpperWatermark
                };

                await runLog.WriteAsync(new SyncRunLogEntry
                {
                    SourceTable = config.SourceTable,
                    Mode = "ChangeTracking",
                    Outcome = SyncStatus.Outcome.NoChanges,
                    CheckpointBefore = checkpoint.LastSyncVersion,
                    CheckpointAfter = batch.UpperWatermark,
                    StartedAt = startedAt,
                    FinishedAt = DateTime.UtcNow,
                    DurationMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds
                }, cancellationToken);

                return noChangesResult;
            }

            // Apply the change batch with retry for transient PostgreSQL failures
            var result = await ApplyWithRetryAsync(config, batch, cancellationToken);

            // If the applier detected an optimistic concurrency failure,
            // transition the table to requires_full_resync
            if (result.Outcome == SyncStatus.Outcome.RequiresFullResync)
            {
                await checkpointStore.TransitionToFullResyncAsync(
                    config.SourceTable,
                    result.ErrorCode,
                    result.ErrorMessage,
                    cancellationToken);
            }

            var finishedAt = DateTime.UtcNow;
            await runLog.WriteAsync(new SyncRunLogEntry
            {
                SourceTable = config.SourceTable,
                Mode = "ChangeTracking",
                Outcome = result.Outcome,
                RowsRead = batch.Rows.Count,
                RowsUpserted = result.RowsUpserted,
                RowsDeactivated = result.RowsDeactivated,
                RowsDeleted = result.RowsDeleted,
                CheckpointBefore = checkpoint.LastSyncVersion,
                CheckpointAfter = result.CheckpointAfter ?? batch.UpperWatermark,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                DurationMs = (int)(finishedAt - startedAt).TotalMilliseconds,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage,
                RowDetailsJson = result.RowDetailsJson
            }, cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("CT sync cancelled for {SourceTable}", config.SourceTable);
            await runLog.WriteAsync(
                CreateFailedEntry(config.SourceTable, startedAt, "Cancelled", "Operation was cancelled"),
                CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CT sync failed for {SourceTable}", config.SourceTable);
            await runLog.WriteAsync(
                CreateFailedEntry(config.SourceTable, startedAt, "CTSyncFailed", ex.Message),
                CancellationToken.None);

            return new SyncRunResult
            {
                Outcome = SyncStatus.Outcome.Failed,
                ErrorCode = "CTSyncFailed",
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<SyncRunResult> ApplyWithRetryAsync(
        TableSyncConfig config,
        ChangeBatch batch,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxApplyRetries; attempt++)
        {
            try
            {
                return await applier.ApplyBatchAsync(config, batch, cancellationToken);
            }
            catch (Exception ex) when (attempt < MaxApplyRetries && IsTransient(ex))
            {
                logger.LogWarning(ex,
                    "Transient error applying CT batch for {SourceTable} (attempt {Attempt}/{MaxRetries}), retrying...",
                    config.SourceTable, attempt, MaxApplyRetries);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                    cancellationToken);
            }
            catch (Exception ex) when (attempt == MaxApplyRetries && IsTransient(ex))
            {
                logger.LogError(ex,
                    "Transient error applying CT batch for {SourceTable} — all {MaxRetries} attempts exhausted",
                    config.SourceTable, MaxApplyRetries);

                return new SyncRunResult
                {
                    Outcome = SyncStatus.Outcome.Failed,
                    ErrorCode = "ApplyRetriesExhausted",
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex) when (!IsTransient(ex))
            {
                logger.LogError(ex,
                    "Non-transient error applying CT batch for {SourceTable} — failing fast",
                    config.SourceTable);

                return new SyncRunResult
                {
                    Outcome = SyncStatus.Outcome.Failed,
                    ErrorCode = "NonTransientApplyError",
                    ErrorMessage = ex.Message
                };
            }
        }

        return new SyncRunResult
        {
            Outcome = SyncStatus.Outcome.Failed,
            ErrorCode = "ApplyUnexpected",
            ErrorMessage = "ApplyWithRetryAsync exited without returning a result"
        };
    }

    private static bool IsTransient(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection", StringComparison.OrdinalIgnoreCase);
    }

    private static SyncRunLogEntry CreateFailedEntry(
        string sourceTable, DateTime startedAt, string errorCode, string errorMessage)
    {
        var now = DateTime.UtcNow;
        SyncGuard.AssertValidOutcome(
            SyncStatus.Outcome.Failed, nameof(SyncStatus.Outcome));
        return new SyncRunLogEntry
        {
            SourceTable = sourceTable,
            Mode = "ChangeTracking",
            Outcome = SyncStatus.Outcome.Failed,
            StartedAt = startedAt,
            FinishedAt = now,
            DurationMs = (int)(now - startedAt).TotalMilliseconds,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
