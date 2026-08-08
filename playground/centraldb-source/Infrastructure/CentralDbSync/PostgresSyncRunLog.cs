namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;

public sealed class PostgresSyncRunLog(
    string connectionString)
    : ISyncRunLog
{
    private const string TableName = "sync_meta.sync_run_log";

    public async Task WriteAsync(
        SyncRunLogEntry entry,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        await conn.ExecuteAsync(
            $@"INSERT INTO {TableName}
               (source_table, run_id, mode, outcome,
                rows_read, rows_upserted, rows_deactivated, rows_deleted,
                checkpoint_before, checkpoint_after,
                started_at, finished_at, duration_ms,
                error_code, error_message)
               VALUES
               (@SourceTable, @RunId, @Mode, @Outcome,
                @RowsRead, @RowsUpserted, @RowsDeactivated, @RowsDeleted,
                @CheckpointBefore, @CheckpointAfter,
                @StartedAt, @FinishedAt, @DurationMs,
                @ErrorCode, @ErrorMessage)",
            entry);
    }
}
