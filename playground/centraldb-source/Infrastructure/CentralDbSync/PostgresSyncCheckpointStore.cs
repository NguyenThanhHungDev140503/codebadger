namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;

public sealed class PostgresSyncCheckpointStore(
    string connectionString)
    : ISyncCheckpointStore
{
    private const string TableName = "sync_meta.checkpoint";

    private static readonly string SelectColumns = string.Join(", ",
        "source_table AS SourceTable",
        "last_sync_version AS LastSyncVersion",
        "sync_status AS SyncStatus",
        "last_attempt_at AS LastAttemptAt",
        "last_success_at AS LastSuccessAt",
        "last_failure_at AS LastFailureAt",
        "consecutive_failure_count AS ConsecutiveFailureCount",
        "last_error_code AS LastErrorCode",
        "last_error_message AS LastErrorMessage");

    public async Task<SyncCheckpointState?> GetAsync(
        string sourceTable,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        var state = await conn.QueryFirstOrDefaultAsync<SyncCheckpointState>(
            $"SELECT {SelectColumns} FROM {TableName} WHERE source_table = @sourceTable",
            new { sourceTable });

        return state;
    }

    public async Task<bool> AdvanceAsync(
        string sourceTable,
        long previousCheckpoint,
        long nextCheckpoint,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        var affected = await conn.ExecuteAsync(
            $@"UPDATE {TableName}
               SET last_sync_version = @nextCheckpoint,
                   sync_status = @syncStatus,
                   last_attempt_at = NOW(),
                   last_success_at = NOW(),
                   consecutive_failure_count = 0,
                   last_error_code = NULL,
                   last_error_message = NULL
               WHERE source_table = @sourceTable
                 AND last_sync_version = @previousCheckpoint",
            new
            {
                sourceTable,
                previousCheckpoint,
                nextCheckpoint,
                syncStatus = SyncStatus.CheckpointState.Ready
            });

        return affected > 0;
    }

    public async Task TransitionToFullResyncAsync(
        string sourceTable,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        await conn.ExecuteAsync(
            $@"UPDATE {TableName}
               SET sync_status = @syncStatus,
                   last_attempt_at = NOW(),
                   last_failure_at = NOW(),
                   last_error_code = @errorCode,
                   last_error_message = @errorMessage,
                   consecutive_failure_count = consecutive_failure_count + 1
               WHERE source_table = @sourceTable",
            new
            {
                syncStatus = SyncStatus.CheckpointState.RequiresFullResync,
                sourceTable,
                errorCode,
                errorMessage
            });
    }
}
