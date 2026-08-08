namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Infrastructure.CentralDbSync.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

/// <summary>
/// Keyset-based batch reader for the scalable parent-child bootstrap flow.
/// Each read opens a short-lived ReadCommitted connection.
/// Does not use READPAST — a locked row whose writer later rolls back would
/// be silently skipped without a CT event to repair it.
/// </summary>
public sealed class SqlServerStagedBootstrapReader(
    string connectionString,
    SqlServerSqlBuilder sqlBuilder,
    ILogger<SqlServerStagedBootstrapReader> logger)
    : IStagedBootstrapSourceReader
{
    public async Task<BootstrapSourcePreflight> ValidateAndCaptureBaselineAsync(
        TableMappingRule rule, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            // Validate CT is enabled for the source table
            var tableName = SqlServerSqlBuilder.QuoteSqlServerTable(rule.Source.PrimaryTable);
            var ctRow = await conn.QueryFirstOrDefaultAsync(
                new CommandDefinition(
                    @"SELECT 1 FROM sys.change_tracking_tables
                      WHERE object_id = OBJECT_ID(@tableName)",
                    new { tableName },
                    cancellationToken: ct));

            if (ctRow is null)
            {
                return BootstrapSourcePreflight.Fail(
                    "CtDisabled",
                    $"Change Tracking is not enabled on {rule.Source.PrimaryTable}.");
            }

            // Capture C0 (baseline version)
            var baseline = await conn.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    "SELECT CHANGE_TRACKING_CURRENT_VERSION()",
                    cancellationToken: ct));

            // Validate C0 is >= MIN_VALID_VERSION
            var minValid = await conn.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    "SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(@tableName))",
                    new { tableName },
                    cancellationToken: ct));

            if (minValid.HasValue && baseline < minValid.Value)
            {
                return BootstrapSourcePreflight.Fail(
                    "CtHistoryExpired",
                    $"CT baseline version {baseline} is below minimum valid version {minValid.Value} " +
                    $"for {rule.Source.PrimaryTable}. Increase CT retention or reduce bootstrap duration.");
            }

            // Approximate row count
            var totalRows = await conn.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    $"SELECT COUNT_BIG(*) FROM {SqlServerSqlBuilder.QuoteSqlServerTable(rule.Source.PrimaryTable)} AS {SqlServerSqlBuilder.QuoteSqlServerColumnReference(rule.Source.PrimaryAlias, rule.Source.PrimaryAlias).Split('.')[0]}",
                    cancellationToken: ct));

            logger.LogInformation(
                "Scalable preflight passed for {RuleName}: C0={Baseline}, totalRows={TotalRows}",
                rule.RuleName, baseline, totalRows);

            return BootstrapSourcePreflight.Valid(baseline, totalRows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Scalable preflight failed for {RuleName}", rule.RuleName);
            return BootstrapSourcePreflight.Fail(
                "PreflightFailed",
                $"Scalable bootstrap preflight failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<GenericSourceRow>> ReadBatchAsync(
        TableMappingRule rule, object? afterKey, int batchSize, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, ct);

        try
        {
            // Use BuildKeysetBootstrapSelect which generates the correct SQL
            // with keyset pagination, active predicate columns, and proper aliases.
            var select = sqlBuilder.BuildKeysetBootstrapSelect(rule, afterKey, batchSize);

            await using var cmd = new SqlCommand(select.Sql, conn, tx);
            foreach (var name in select.Parameters.ParameterNames)
            {
                cmd.Parameters.AddWithValue("@" + name,
                    select.Parameters.Get<object?>(name) ?? DBNull.Value);
            }

            var rows = new List<GenericSourceRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var alias in select.ColumnAliases)
                {
                    values[alias] = reader[alias] is DBNull ? null : reader[alias];
                }
                rows.Add(new GenericSourceRow(values));
            }

            logger.LogDebug(
                "Read {RowCount} rows for {RuleName} afterKey={AfterKey} (batchSize={BatchSize})",
                rows.Count, rule.RuleName, afterKey, batchSize);

            return rows;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<long> GetCurrentVersionAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(
                "SELECT CHANGE_TRACKING_CURRENT_VERSION()",
                cancellationToken: ct));
    }

    public async Task<CtDeltaResult> ReadCtDeltaAsync(
        TableMappingRule rule, long baselineVersion, long watermarkVersion, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            var ctSelect = sqlBuilder.BuildChangeTrackingSelect(rule);
            var parameters = new DynamicParameters(ctSelect.Parameters);
            parameters.Add("checkpoint", baselineVersion);
            parameters.Add("upperWatermark", watermarkVersion);

            // We need to read the raw result set because the query includes
            // SYS_CHANGE_OPERATION at position 0, which is not in ColumnAliases.
            await using var cmd = new SqlCommand(ctSelect.Sql, conn);
            foreach (var name in parameters.ParameterNames)
            {
                cmd.Parameters.AddWithValue("@" + name,
                    parameters.Get<object?>(name) ?? DBNull.Value);
            }

            // Build a set of column names (lowercase) for the data columns.
            // These include mapped columns, transform dependencies, and active
            // predicate columns (added by BuildSelectList).
            var dataCols = ctSelect.ColumnAliases
                .Select(a => a.ToLowerInvariant())
                .ToHashSet();

            var upserts = new List<GenericSourceRow>();
            var deletedPks = new List<object?[]>();

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var operation = reader.GetString(0); // SYS_CHANGE_OPERATION

                if (operation == "D")
                {
                    // For deletes, the base table is LEFT JOINed and may be NULL,
                    // so read PK values from CT result aliases (__ct_pk_N).
                    var pkValues = rule.Source.PrimaryKey
                        .Select((_, index) =>
                        {
                            var alias = SqlServerSqlBuilder.GetCtPrimaryKeyAlias(index);
                            return reader[alias] is DBNull ? null : reader[alias];
                        })
                        .ToArray();
                    deletedPks.Add(pkValues);
                }
                else
                {
                    // I/U: read mapped column values
                    var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var colName = reader.GetName(i);
                        if (dataCols.Contains(colName.ToLowerInvariant()))
                        {
                            values[colName] = reader[i] is DBNull ? null : reader[i];
                        }
                    }

                    // Check ActivePredicate — if the row no longer matches,
                    // treat it as filter-excluded (remove from staging).
                    if (PassesActivePredicate(rule, values))
                    {
                        upserts.Add(new GenericSourceRow(values));
                    }
                    else
                    {
                        var pkValues = rule.Source.PrimaryKey
                            .Select((_, index) =>
                            {
                                var alias = SqlServerSqlBuilder.GetCtPrimaryKeyAlias(index);
                                return reader[alias] is DBNull ? null : reader[alias];
                            })
                            .ToArray();
                        deletedPks.Add(pkValues);
                    }
                }
            }

            logger.LogInformation(
                "CT delta for {RuleName} in ({C0}, {C1}]: {Upserts} upserts, {Deletes} deletes",
                rule.RuleName, baselineVersion, watermarkVersion,
                upserts.Count, deletedPks.Count);

            return CtDeltaResult.Success(upserts, deletedPks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "CT delta read failed for {RuleName} ({C0}, {C1}]",
                rule.RuleName, baselineVersion, watermarkVersion);
            return CtDeltaResult.Fail("CtDeltaReadFailed",
                BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "CT delta read failed.");
        }
    }

    /// <summary>
    /// Evaluates whether a row passes all ActivePredicate conditions.
    /// An empty ActivePredicate list means every row is active (returns true).
    /// </summary>
    private static bool PassesActivePredicate(
        TableMappingRule rule, IReadOnlyDictionary<string, object?> values)
    {
        var predicates = rule.Source.ActivePredicate;
        if (predicates.Count == 0)
            return true;

        foreach (var predicate in predicates)
        {
            // Resolve column name (strip alias prefix if present,
            // e.g. "t0.IsActive" -> "IsActive")
            var column = predicate.Column.Contains('.')
                ? predicate.Column[(predicate.Column.LastIndexOf('.') + 1)..]
                : predicate.Column;

            values.TryGetValue(column, out var actualValue);

            bool matches = predicate.Operator switch
            {
                PredicateOperator.Eq => Equals(actualValue, predicate.Value),
                PredicateOperator.Neq => !Equals(actualValue, predicate.Value),
                PredicateOperator.IsNull => actualValue is null or DBNull,
                PredicateOperator.IsNotNull => actualValue is not null and not DBNull,
                PredicateOperator.Gt => CompareValues(actualValue, predicate.Value) > 0,
                PredicateOperator.Gte => CompareValues(actualValue, predicate.Value) >= 0,
                PredicateOperator.Lt => CompareValues(actualValue, predicate.Value) < 0,
                PredicateOperator.Lte => CompareValues(actualValue, predicate.Value) <= 0,
                PredicateOperator.In => predicate.Value is System.Collections.IEnumerable e
                    && e.Cast<object?>().Any(v => Equals(actualValue, v)),
                PredicateOperator.NotIn => predicate.Value is System.Collections.IEnumerable e
                    && !e.Cast<object?>().Any(v => Equals(actualValue, v)),
                _ => true
            };

            if (!matches)
                return false;
        }

        return true;
    }

    private static int CompareValues(object? a, object? b)
    {
        if (a is IComparable ca && b is not null)
            return ca.CompareTo(b);
        if (b is IComparable cb && a is not null)
            return -cb.CompareTo(a);
        return 0;
    }
}
