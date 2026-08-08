namespace Application.Features.CentralDbSync.Abstractions;

using Application.Features.CentralDbSync.Models;
using System.Data.Common;

/// <summary>
/// Runtime toggle for whether a source table participates in the recurring sync.
/// Backed by sync_meta.table_sync_config.enabled on PostgreSQL.
/// Tables not yet present in the store are treated as disabled (false) — a row
/// is only seeded on bootstrap success, so an absent row means never bootstrapped.
/// </summary>
public interface ISyncConfigStore
{
    /// <summary>
    /// Full upsert of a table sync config row with all fields from <see cref="TableSyncConfig"/>.
    /// Called on bootstrap success to seed the config with accurate metadata.
    /// Opens its own connection. Use <see cref="SeedInTransactionAsync"/> to participate
    /// in an existing transaction.
    /// </summary>
    Task SeedAsync(TableSyncConfig config, CancellationToken ct);

    /// <summary>
    /// Upserts a table sync config row within the caller's open connection and transaction.
    /// Used by scalable final publish to seed config atomically with checkpoint/outbox work.
    /// </summary>
    Task SeedInTransactionAsync(DbConnection connection, DbTransaction transaction,
        TableSyncConfig config, CancellationToken ct);

    /// <summary>
    /// Returns the enabled flag for a source table.
    /// Null/absent row — returns false (disabled).
    /// </summary>
    Task<bool> IsEnabledAsync(string sourceTable, CancellationToken ct);

    /// <summary>
    /// Updates the enabled flag for an existing config row.
    /// The row must have already been seeded (e.g. via <see cref="SeedAsync"/>).
    /// </summary>
    Task SetEnabledAsync(string sourceTable, bool enabled, CancellationToken ct);

    /// <summary>
    /// Returns every table config row that has been explicitly persisted
    /// (source_table → enabled), including rows whose enabled flag is false.
    /// Rules with no row are absent from the result and count as disabled.
    /// </summary>
    Task<IReadOnlyDictionary<string, bool>> GetAllConfiguredAsync(CancellationToken ct);
}
