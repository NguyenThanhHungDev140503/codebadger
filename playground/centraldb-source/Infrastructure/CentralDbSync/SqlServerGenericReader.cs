namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Infrastructure.CentralDbSync.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

public sealed class SqlServerGenericReader(
    string connectionString,
    IMappingRuleProvider ruleProvider,
    SqlServerSqlBuilder sqlBuilder,
    ILogger<SqlServerGenericReader> logger)
    : IBootstrapSnapshotReader, IChangeTrackingReader
{
    private const int MaxBootstrapRetries = 3;

    async Task<BootstrapSnapshot> IBootstrapSnapshotReader.ReadAsync(
        TableSyncConfig config,
        CancellationToken ct)
    {
        var rule = ruleProvider.Get(config.SourceTable);
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        for (var attempt = 1; attempt <= MaxBootstrapRetries; attempt++)
        {
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var baseline = await conn.ExecuteScalarAsync<long>(
                    "SELECT CHANGE_TRACKING_CURRENT_VERSION()",
                    transaction: tx);

                var select = sqlBuilder.BuildBootstrapSelect(rule);
                var rows = await ReadRowsAsync(conn, tx, select, ct);

                var versionAfter = await conn.ExecuteScalarAsync<long>(
                    "SELECT CHANGE_TRACKING_CURRENT_VERSION()",
                    transaction: tx);

                if (baseline == versionAfter)
                {
                    await tx.CommitAsync(ct);
                    logger.LogInformation(
                        "Bootstrap snapshot for {SourceTable}: {RowCount} rows at version {Version}",
                        config.SourceTable, rows.Count, baseline);
                    return new BootstrapSnapshot(baseline, rows);
                }

                logger.LogDebug(
                    "Bootstrap snapshot version changed {Before} -> {After}, retry {Attempt}/{MaxRetries}",
                    baseline, versionAfter, attempt, MaxBootstrapRetries);
                await tx.RollbackAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        throw new InvalidOperationException(
            $"Failed to capture a consistent bootstrap snapshot for {config.SourceTable} after {MaxBootstrapRetries} attempts.");
    }

    async Task<ChangeBatch> IChangeTrackingReader.ReadBatchAsync(
        TableSyncConfig config,
        long checkpoint,
        CancellationToken ct)
    {
        var rule = ruleProvider.Get(config.SourceTable);
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        try
        {
            var minValid = await conn.ExecuteScalarAsync<long?>(
                $"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(N'{SqlServerSqlBuilder.QuoteSqlServerTable(rule.Source.PrimaryTable).Replace("'", "''")}'))",
                transaction: tx);

            if (minValid.HasValue && checkpoint < minValid.Value)
            {
                logger.LogWarning(
                    "Checkpoint {Checkpoint} is below minimum valid version {MinValid} for {SourceTable}",
                    checkpoint, minValid.Value, config.SourceTable);
                throw new CheckpointInvalidException(config.SourceTable, checkpoint, minValid.Value);
            }

            var upperWatermark = await conn.ExecuteScalarAsync<long>(
                "SELECT CHANGE_TRACKING_CURRENT_VERSION()",
                transaction: tx);

            var select = sqlBuilder.BuildChangeTrackingSelect(rule);
            select.Parameters.Add("checkpoint", checkpoint);
            select.Parameters.Add("upperWatermark", upperWatermark);

            var rows = await ReadChangeRowsAsync(conn, tx, rule, select, ct);

            await tx.CommitAsync(ct);

            logger.LogInformation(
                "Read {RowCount} changes for {SourceTable}: checkpoint {Checkpoint} -> {UpperWatermark}",
                rows.Count, config.SourceTable, checkpoint, upperWatermark);

            return new ChangeBatch(checkpoint, upperWatermark, rows);
        }
        catch (CheckpointInvalidException)
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "Failed to read changes for {SourceTable}", config.SourceTable);
            throw;
        }
    }

    private static async Task<IReadOnlyList<GenericSourceRow>> ReadRowsAsync(
        SqlConnection conn,
        SqlTransaction tx,
        SelectSql select,
        CancellationToken ct)
    {
        var rows = new List<GenericSourceRow>();
        await using var cmd = CreateCommand(conn, tx, select);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(ReadSourceRow(reader, select.ColumnAliases));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<GenericChangeRow>> ReadChangeRowsAsync(
        SqlConnection conn,
        SqlTransaction tx,
        TableMappingRule rule,
        SelectSql select,
        CancellationToken ct)
    {
        var rows = new List<GenericChangeRow>();
        await using var cmd = CreateCommand(conn, tx, select);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var operation = (string)reader["SYS_CHANGE_OPERATION"];
            var changeVersion = (long)reader["SYS_CHANGE_VERSION"];
            var primaryKey = Enumerable.Range(0, rule.Source.PrimaryKey.Count)
                .Select(index => NormalizeDbValue(reader[SqlServerSqlBuilder.GetCtPrimaryKeyAlias(index)]))
                .ToList();
            var currentValues = operation == "D"
                ? null
                : ReadSourceRow(reader, select.ColumnAliases);

            rows.Add(new GenericChangeRow(operation, changeVersion, primaryKey, currentValues));
        }

        return rows;
    }

    private static GenericSourceRow ReadSourceRow(SqlDataReader reader, IReadOnlyList<string> aliases)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in aliases)
        {
            values[alias] = NormalizeDbValue(reader[alias]);
        }

        return new GenericSourceRow(values);
    }

    private static SqlCommand CreateCommand(
        SqlConnection conn,
        SqlTransaction tx,
        SelectSql select)
    {
        var command = new SqlCommand(select.Sql, conn, tx);
        foreach (var name in select.Parameters.ParameterNames)
        {
            command.Parameters.AddWithValue("@" + name, select.Parameters.Get<object?>(name) ?? DBNull.Value);
        }

        return command;
    }

    private static object? NormalizeDbValue(object? value)
        => value is DBNull ? null : value;
}
