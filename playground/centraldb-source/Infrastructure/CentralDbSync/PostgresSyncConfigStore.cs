namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;
using System.Data.Common;

public sealed class PostgresSyncConfigStore(string connectionString)
    : ISyncConfigStore
{
    private const string TableName = "sync_meta.table_sync_config";

    public async Task SeedAsync(TableSyncConfig config, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await SeedCoreAsync(conn, null, config, ct);
    }

    public async Task SeedInTransactionAsync(DbConnection connection, DbTransaction transaction,
        TableSyncConfig config, CancellationToken ct)
    {
        await SeedCoreAsync(connection, transaction, config, ct);
    }

    private static async Task SeedCoreAsync(DbConnection connection, DbTransaction? transaction,
        TableSyncConfig config, CancellationToken ct)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                $@"INSERT INTO {TableName}
                    (source_table, target_schema, target_table, sync_mode, sync_tier,
                     dependency, expected_sync_interval, max_allowed_lag, ownership_scope, enabled)
                    VALUES
                    (@sourceTable, @targetSchema, @targetTable, @syncMode, @syncTier,
                     @dependency, @expectedSyncInterval, @maxAllowedLag, @ownershipScope, true)
                    ON CONFLICT (source_table)
                    DO UPDATE SET
                        target_schema          = EXCLUDED.target_schema,
                        target_table           = EXCLUDED.target_table,
                        sync_mode              = EXCLUDED.sync_mode,
                        sync_tier              = EXCLUDED.sync_tier,
                        dependency             = EXCLUDED.dependency,
                        expected_sync_interval = EXCLUDED.expected_sync_interval,
                        max_allowed_lag        = EXCLUDED.max_allowed_lag,
                        ownership_scope        = EXCLUDED.ownership_scope,
                        enabled                = true",
                new
                {
                    sourceTable          = config.SourceTable,
                    targetSchema         = config.TargetSchema,
                    targetTable          = config.TargetTable,
                    syncMode             = config.SyncMode,
                    syncTier             = config.SyncTier,
                    dependency           = config.Dependency,
                    expectedSyncInterval = config.ExpectedSyncInterval,
                    maxAllowedLag        = config.MaxAllowedLag,
                    ownershipScope       = config.OwnershipScope
                },
                transaction: transaction,
                cancellationToken: ct));
    }

    public async Task<bool> IsEnabledAsync(string sourceTable, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        var enabled = await conn.QueryFirstOrDefaultAsync<bool?>(
            $"SELECT enabled FROM {TableName} WHERE source_table = @sourceTable",
            new { sourceTable });

        // Absent row = never seeded by a successful bootstrap = not syncable
        return enabled ?? false;
    }

    public async Task SetEnabledAsync(string sourceTable, bool enabled, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        var affected = await conn.ExecuteAsync(
            $"UPDATE {TableName} SET enabled = @enabled WHERE source_table = @sourceTable",
            new { sourceTable, enabled });

        // If no row exists, the table hasn't been seeded yet. Bootstrap must run first.
        if (affected == 0)
        {
            throw new InvalidOperationException(
                $"Cannot toggle '{sourceTable}': no row in table_sync_config. " +
                "The table has not been seeded — run bootstrap first.");
        }
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllConfiguredAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        var rows = await conn.QueryAsync<SyncConfigRow>(
            $"SELECT source_table AS SourceTable, enabled AS Enabled FROM {TableName}",
            new { });

        return new Dictionary<string, bool>(
            rows.Select(r => KeyValuePair.Create(r.SourceTable, r.Enabled)),
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed record SyncConfigRow(string SourceTable, bool Enabled);
}
