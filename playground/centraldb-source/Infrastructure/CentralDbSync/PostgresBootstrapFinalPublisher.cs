namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

/// <summary>
/// Atomically publishes staged data to the rule's target table and advances the
/// checkpoint to C1 — all in a single PostgreSQL transaction that also DROPs the
/// dynamic staging table. DDL is transactional in PostgreSQL; a rollback leaves
/// the stage table intact for retry.
/// </summary>
public sealed class PostgresBootstrapFinalPublisher(
    string centralDbConnection,
    ITypedBootstrapStagingStore stagingStore,
    ISyncConfigStore configStore,
    IBootstrapCtDispatchStore ctDispatchStore,
    ILogger<PostgresBootstrapFinalPublisher> logger) : IBootstrapFinalPublisher
{
    public async Task<FinalPublishResult> PublishAsync(
        TableMappingRule rule,
        Guid parentId,
        Guid fencingToken,
        string stagingSchema,
        string stagingTableName,
        long baselineVersion,
        long watermarkVersion,
        Guid? bootstrapRequestId,
        CancellationToken ct)
    {
        var pkColumns = rule.Target.PrimaryKey;
        var targetColumns = rule.Columns
            .Where(c => !string.IsNullOrWhiteSpace(c.TargetColumn))
            .ToList();

        if (targetColumns.Count == 0)
        {
            return FinalPublishResult.Fail("NoTargetColumns",
                "Rule has no target columns defined");
        }

        var targetTable = $"{QuoteIdent(rule.Target.Schema)}.{QuoteIdent(rule.Target.Table)}";
        var stageTable = $"{QuoteIdent(stagingSchema)}.{QuoteIdent(stagingTableName)}";
        var colNames = string.Join(", ", targetColumns.Select(c => QuoteIdent(c.TargetColumn)));
        var pkJoin = string.Join(" AND ",
            pkColumns.Select(pk =>
                $"{targetTable}.{QuoteIdent(pk)} = {stageTable}.{QuoteIdent(pk)}"));

        // Build the set-based upsert SQL
        var updateSet = string.Join(", ",
            targetColumns
                .Where(c => !c.IsPrimaryKey)
                .Select(c => $"{QuoteIdent(c.TargetColumn)} = EXCLUDED.{QuoteIdent(c.TargetColumn)}"));

        var insertValues = string.Join(", ", targetColumns.Select(c => $"{stageTable}.{QuoteIdent(c.TargetColumn)}"));

        var upsertSql = $"""
            INSERT INTO {targetTable} ({colNames})
            SELECT {insertValues}
            FROM {stageTable}
            ON CONFLICT ({string.Join(", ", pkColumns.Select(pk => QuoteIdent(pk)))})
            DO UPDATE SET {updateSet}
            """;

        // Lifecycle for target rows absent from the stage. The branch depends only on
        // whether an active-flag column exists, matching UpsertSqlBuilder on the CT path:
        // with a flag the row is soft-deactivated, without one it is removed by primary key.
        // Branching on ActivePredicate instead left rules that declare a predicate but no
        // flag — such as CRM.Partners-to-customer — with no lifecycle handling at all.
        var orphanFilter = $"""
            WHERE NOT EXISTS (
                SELECT 1 FROM {stageTable}
                WHERE {pkJoin}
            )
            """;

        var activeFlagCol = targetColumns.FirstOrDefault(c => c.IsActiveFlag);
        var lifecycleSql = activeFlagCol is not null
            ? $"""
               UPDATE {targetTable}
               SET {QuoteIdent(activeFlagCol.TargetColumn)} = FALSE
               {orphanFilter}
               """
            : $"""
               DELETE FROM {targetTable}
               {orphanFilter}
               """;

        await using var conn = new NpgsqlConnection(centralDbConnection);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // 1. Set statement_timeout for this transaction
            await conn.ExecuteAsync("SET LOCAL statement_timeout = '1800000'", transaction: tx);

            // The publisher only sees the stage, so the source row count and the true start
            // of the bootstrap have to come from the parent. Aliases are quoted because
            // PostgreSQL lower-cases unquoted ones and Dapper would not bind them.
            var progress = await conn.QuerySingleOrDefaultAsync<BootstrapProgress>("""
                SELECT COALESCE(rows_staged, 0) AS "RowsStaged",
                       created_at              AS "StartedAt"
                FROM sync_meta.bootstrap_parent
                WHERE parent_id = @ParentId
                """, new { ParentId = parentId }, transaction: tx);

            // 2. Upsert staging → target
            var upserted = await conn.ExecuteAsync(upsertSql, transaction: tx);

            // 3. Handle lifecycle (orphans)
            var lifecycleRows = await conn.ExecuteAsync(lifecycleSql, transaction: tx);
            var deactivated = activeFlagCol is not null ? lifecycleRows : 0;
            var deleted = activeFlagCol is not null ? 0 : lifecycleRows;

            // 4. Advance checkpoint in the same transaction.
            // Upsert rather than update: nothing in the engine creates a checkpoint row for
            // a rule bootstrapped through this path, and 002-central-db-sync-seed.sql only
            // seeds CRM.Partners, so a plain UPDATE matches nothing on a first bootstrap.
            // The conflict guard still refuses to rewind a checkpoint that a concurrent sync
            // has already advanced beyond this bootstrap's watermark.
            var checkpointAdvanced = await conn.ExecuteAsync("""
                INSERT INTO sync_meta.checkpoint AS c
                    (source_table, last_sync_version, sync_status,
                     last_attempt_at, last_success_at, consecutive_failure_count)
                VALUES (@source, @next, @ready, NOW(), NOW(), 0)
                ON CONFLICT (source_table) DO UPDATE
                SET last_sync_version = EXCLUDED.last_sync_version,
                    sync_status = EXCLUDED.sync_status,
                    last_attempt_at = NOW(),
                    last_success_at = NOW(),
                    consecutive_failure_count = 0,
                    last_error_code = NULL,
                    last_error_message = NULL
                WHERE c.last_sync_version IS NULL
                   OR c.last_sync_version <= @next
                """, new
            {
                source = rule.RuleName,
                next = watermarkVersion,
                ready = SyncStatus.CheckpointState.Ready
            }, transaction: tx);

            if (checkpointAdvanced == 0)
            {
                await tx.RollbackAsync(ct);
                return FinalPublishResult.Fail("CheckpointConflict",
                    $"Checkpoint for {rule.RuleName} is already past bootstrap watermark {watermarkVersion}");
            }

            // 5. Write run log
            var startedAt = progress?.StartedAt ?? DateTime.UtcNow;
            var finishedAt = DateTime.UtcNow;

            await conn.ExecuteAsync("""
                INSERT INTO sync_meta.sync_run_log
                    (source_table, run_id, mode, outcome,
                     rows_read, rows_upserted, rows_deactivated, rows_deleted,
                     checkpoint_before, checkpoint_after,
                     started_at, finished_at, duration_ms)
                VALUES
                    (@Source, @RunId, @Mode, @Outcome,
                     @RowsRead, @RowsUpserted, @RowsDeactivated, @RowsDeleted,
                     @CkBefore, @CkAfter,
                     @Started, @Finished, @DurationMs)
                """, new
            {
                Source = rule.RuleName,
                RunId = parentId,
                Mode = "Bootstrap",
                Outcome = "Succeeded",
                RowsRead = progress?.RowsStaged ?? 0,
                RowsUpserted = upserted,
                RowsDeactivated = deactivated,
                RowsDeleted = deleted,
                CkBefore = baselineVersion,
                CkAfter = watermarkVersion,
                Started = startedAt,
                Finished = finishedAt,
                DurationMs = (int)Math.Clamp(
                    (finishedAt - startedAt).TotalMilliseconds, 0, int.MaxValue)
            }, transaction: tx);

            // 6. Mark parent Completed (CAS: fencing token + status guard)
            var parentUpdated = await conn.ExecuteAsync("""
                UPDATE sync_meta.bootstrap_parent
                SET status = @CompletedStatus, last_heartbeat_at = NOW()
                WHERE parent_id = @ParentId
                  AND fencing_token = @FencingToken
                  AND status = @PublishingStatus
                """, new
            {
                ParentId = parentId,
                FencingToken = fencingToken,
                PublishingStatus = BootstrapParentStatus.Publishing,
                CompletedStatus = BootstrapParentStatus.Completed
            }, transaction: tx);

            if (parentUpdated == 0)
            {
                await tx.RollbackAsync(ct);
                return FinalPublishResult.Fail("StaleWorker",
                    $"Parent {parentId} could not be marked Completed — fencing token or status mismatch (stale worker).");
            }

            // 6.5. Mark bootstrap_request as Completed (if linked)
            if (bootstrapRequestId.HasValue)
            {
                await conn.ExecuteAsync(@"
                    UPDATE sync_meta.bootstrap_request
                    SET status = 'completed', finished_at = NOW()
                    WHERE request_id = @RequestId
                      AND status IN ('queued', 'running')
                    ", new { RequestId = bootstrapRequestId.Value }, transaction: tx);
            }

            // 7. Seed table_sync_config in the same transaction so the rule is
            //    immediately active for cron-based sync after bootstrap completes
            await configStore.SeedInTransactionAsync(conn, tx, rule.ToTableSyncConfig(), ct);

            // 8. Insert CT outbox marker via the dispatch store
            await ctDispatchStore.CreateInTransactionAsync(
                conn, tx, rule.RuleName, parentId, watermarkVersion, ct);

            // 9. Drop staging table atomically
            await stagingStore.DropStageAsync(conn, tx, stagingSchema, stagingTableName, ct);

            await tx.CommitAsync(ct);

            logger.LogInformation(
                "Published {Rows} rows to {Target} for rule {RuleName} (parent {ParentId})",
                upserted, targetTable, rule.RuleName, parentId);

            return FinalPublishResult.Success(upserted, deactivated, deleted, watermarkVersion);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex,
                "Final publish failed for parent {ParentId}, rule {RuleName}. Stage table preserved.",
                parentId, rule.RuleName);
            return FinalPublishResult.Fail("PublishFailed",
                BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Final publish failed.");
        }
    }

    private static string QuoteIdent(string ident)
        => $"\"{ident.Replace("\"", "\"\"")}\"";

    /// <summary>Bootstrap totals owned by the parent row, needed for the run log.</summary>
    private sealed record BootstrapProgress(long RowsStaged, DateTime StartedAt);
}
