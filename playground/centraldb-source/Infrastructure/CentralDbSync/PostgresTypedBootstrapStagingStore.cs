namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Infrastructure.CentralDbSync.Sql;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;

/// <summary>
/// Manages dynamic per-parent staging tables for the scalable bootstrap flow.
/// DDL is generated from <see cref="TableMappingRule"/> column definitions.
/// Uses a TEMP batch table pattern for idempotent COPY with ON CONFLICT.
/// </summary>
public sealed class PostgresTypedBootstrapStagingStore(
    string connectionString,
    IMappingRuleProvider ruleProvider,
    ILogger<PostgresTypedBootstrapStagingStore> logger)
    : ITypedBootstrapStagingStore
{
    public async Task CreateStageAsync(Guid parentId, string stagingTableName,
        TableMappingRule rule, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var columnDefs = string.Join(",\n    ",
            rule.Columns.Select(col =>
            {
                var pgType = PostgresTypeMap.ToDdlType(col.TargetType);
                var nullable = col.IsPrimaryKey ? "NOT NULL" : "NULL";
                return $"{QuoteIdent(col.TargetColumn)} {pgType} {nullable}";
            }));

        var pkColumns = rule.Target.PrimaryKey
            .Select(pk => QuoteIdent(pk));

        var sql = $@"
            CREATE TABLE IF NOT EXISTS sync_meta.{QuoteIdent(stagingTableName)}
            (
                {columnDefs},
                PRIMARY KEY ({string.Join(", ", pkColumns)})
            )";

        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));

        logger.LogInformation(
            "Created dynamic staging table sync_meta.{StageTable} for parent {ParentId}",
            stagingTableName, parentId);
    }

    public async Task StageBatchAsync(TableMappingRule rule, string stagingSchema, string stagingTableName,
        IReadOnlyList<GenericSourceRow> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {

            // Step 1: Create TEMP batch table (ON COMMIT DROP)
            var tempTableName = $"bs_batch_{Guid.NewGuid():N}";
            var createTempSql = $@"
                CREATE TEMP TABLE {QuoteIdent(tempTableName)} (LIKE {QuoteIdent(stagingSchema)}.{QuoteIdent(stagingTableName)})
                ON COMMIT DROP";
            await conn.ExecuteAsync(createTempSql, transaction: tx);

            // Step 2: Binary COPY into TEMP table
            var columns = rule.Columns.Where(c => !c.IsActiveFlag && string.IsNullOrWhiteSpace(c.Transform)).ToList();
            await using (var writer = await conn.BeginBinaryImportAsync(
                $"COPY {QuoteIdent(tempTableName)} ({string.Join(", ", columns.Select(c => QuoteIdent(c.TargetColumn)))}) FROM STDIN (FORMAT BINARY)",
                ct))
            {
                foreach (var row in rows)
                {
                    await writer.StartRowAsync(ct);
                    foreach (var col in columns)
                    {
                        var value = ResolveColumnValue(rule, col, row);
                        if (value is null)
                        {
                            await writer.WriteNullAsync(ct);
                            continue;
                        }

                        await PostgresTypeMap.WriteCopyValueAsync(writer, col.TargetType, value, ct);
                    }
                }

                await writer.CompleteAsync(ct);
            }

            // Step 3: INSERT from TEMP into staging with ON CONFLICT
            var insertColumns = string.Join(", ", columns.Select(c => QuoteIdent(c.TargetColumn)));
            var updateSet = string.Join(", ",
                columns.Where(c => !c.IsPrimaryKey)
                    .Select(c => $"{QuoteIdent(c.TargetColumn)} = EXCLUDED.{QuoteIdent(c.TargetColumn)}"));
            var pkColumnNames = string.Join(", ",
                rule.Target.PrimaryKey.Select(QuoteIdent));

            var upsertSql = $@"
                INSERT INTO {QuoteIdent(stagingSchema)}.{QuoteIdent(stagingTableName)} ({insertColumns})
                SELECT {insertColumns} FROM {QuoteIdent(tempTableName)}
                ON CONFLICT ({pkColumnNames}) DO UPDATE SET {updateSet}";

            await conn.ExecuteAsync(upsertSql, transaction: tx);

            await tx.CommitAsync(ct);

            logger.LogDebug(
                "Staged {RowCount} rows into sync_meta.{StageTable}",
                rows.Count, stagingTableName);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteStageRowsAsync(TableMappingRule rule, string stagingSchema, string stagingTableName,
        IReadOnlyList<object?[]> primaryKeyValues, CancellationToken ct)
    {
        if (primaryKeyValues.Count == 0) return;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var pkColumns = rule.Target.PrimaryKey;
        var whereFragments = new List<string>(primaryKeyValues.Count);
        var parameters = new DynamicParameters();

        for (var i = 0; i < primaryKeyValues.Count; i++)
        {
            var pkValues = primaryKeyValues[i];
            var colFragments = new List<string>(pkColumns.Count);
            for (var j = 0; j < pkColumns.Count; j++)
            {
                var paramName = $"@p{i}_{j}";
                colFragments.Add($"{QuoteIdent(pkColumns[j])} = {paramName}");
                parameters.Add(paramName,
                    j < pkValues.Length ? pkValues[j] : null);
            }
            whereFragments.Add($"({string.Join(" AND ", colFragments)})");
        }

        var sql = $@"
            DELETE FROM {QuoteIdent(stagingSchema)}.{QuoteIdent(stagingTableName)}
            WHERE {string.Join(" OR ", whereFragments)}";

        await conn.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));

        logger.LogDebug(
            "Deleted {Count} rows from sync_meta.{StageTable}",
            primaryKeyValues.Count, stagingTableName);
    }

    public async Task<long> CountAsync(string stagingSchema, string stagingTableName,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(
                $"SELECT COUNT(*) FROM {QuoteIdent(stagingSchema)}.{QuoteIdent(stagingTableName)}",
                cancellationToken: ct));
    }

    public async Task DropStageAsync(DbConnection connection, DbTransaction transaction,
        string stagingSchema, string stagingTableName, CancellationToken ct)
    {
        var sql = $"DROP TABLE IF EXISTS {QuoteIdent(stagingSchema)}.{QuoteIdent(stagingTableName)}";
        await connection.ExecuteAsync(sql, transaction: transaction);

        logger.LogDebug(
            "Dropped dynamic staging table {Schema}.{Table}",
            stagingSchema, stagingTableName);
    }

    public async Task<int> DropOrphanStagesAsync(string schemaPattern, TimeSpan retention,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // List orphan staging tables older than retention
        var tables = await conn.QueryAsync<(string Schema, string Name)>(
            new CommandDefinition(
                @"SELECT bs.schemaname AS Schema, bs.tablename AS Name
                  FROM pg_tables bs
                  LEFT JOIN sync_meta.bootstrap_parent p
                    ON p.staging_table_name = bs.tablename
                  WHERE bs.schemaname = @Schema
                    AND bs.tablename LIKE @Pattern
                    AND (p.parent_id IS NULL
                         OR (p.status IN ('failed', 'completed', 'expired')
                             AND p.cleanup_completed_at IS NULL
                             AND p.completed_at < @Cutoff))
                  LIMIT 100",
                new { Schema = schemaPattern, Pattern = "bs_%", Cutoff = DateTime.UtcNow.Add(-retention) },
                cancellationToken: ct));

        var dropped = 0;
        foreach (var (schema, table) in tables)
        {
            await conn.ExecuteAsync(
                $"DROP TABLE IF EXISTS {QuoteIdent(schema)}.{QuoteIdent(table)}");
            dropped++;
        }

        return dropped;
    }

    private static object? ResolveColumnValue(TableMappingRule rule, ColumnMapping column, GenericSourceRow sourceRow)
    {
        if (!string.IsNullOrWhiteSpace(column.SourceColumn))
            return sourceRow.GetValueOrDefault(column.SourceColumn);

        if (!string.IsNullOrWhiteSpace(column.SourceExpression))
            return sourceRow.GetValueOrDefault(column.TargetColumn);

        return sourceRow.GetValueOrDefault(column.TargetColumn);
    }

    private static string QuoteIdent(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
