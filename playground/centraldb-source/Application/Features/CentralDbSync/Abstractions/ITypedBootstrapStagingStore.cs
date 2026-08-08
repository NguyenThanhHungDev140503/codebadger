using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using System.Data.Common;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Manages the full lifecycle of a dynamic per-parent staging table:
/// create, write (binary COPY + upsert), delete by PK, and drop.
/// </summary>
public interface ITypedBootstrapStagingStore
{
    /// <summary>
    /// DDL: Creates a dynamic staging table from <see cref="TableMappingRule"/>
    /// column definitions. The table name is derived from the parent UUID.
    /// </summary>
    Task CreateStageAsync(Guid parentId, string stagingTableName,
        TableMappingRule rule, CancellationToken ct);

    /// <summary>
    /// DML: Binary COPY a child batch into the dynamic staging table using
    /// a TEMP batch table pattern for idempotent upsert via ON CONFLICT.
    /// </summary>
    Task StageBatchAsync(TableMappingRule rule, string stagingSchema, string stagingTableName,
        IReadOnlyList<GenericSourceRow> rows, CancellationToken ct);

    /// <summary>
    /// DML: Delete rows from staging by primary key values.
    /// Used by CT catch-up to remove source-deleted or filter-excluded rows.
    /// </summary>
    Task DeleteStageRowsAsync(TableMappingRule rule, string stagingSchema, string stagingTableName,
        IReadOnlyList<object?[]> primaryKeyValues, CancellationToken ct);

    /// <summary>Returns the current row count of the staging table.</summary>
    Task<long> CountAsync(string stagingSchema, string stagingTableName,
        CancellationToken ct);

    /// <summary>
    /// DDL: Drops the dynamic staging table. Must accept the caller's
    /// connection and transaction so DROP is atomic with
    /// upsert/checkpoint/metadata writes.
    /// </summary>
    Task DropStageAsync(DbConnection connection, DbTransaction transaction,
        string stagingSchema, string stagingTableName, CancellationToken ct);

    /// <summary>
    /// Cleanup: Drops orphan staging tables for terminal parents older than retention.
    /// Returns the number of tables dropped.
    /// </summary>
    Task<int> DropOrphanStagesAsync(string schemaPattern, TimeSpan retention,
        CancellationToken ct);
}
