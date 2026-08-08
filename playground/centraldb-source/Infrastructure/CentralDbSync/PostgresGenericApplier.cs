namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Infrastructure.CentralDbSync.Sql;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

public sealed class PostgresGenericApplier(
    string connectionString,
    IMappingRuleProvider ruleProvider,
    IValueTransformerRegistry transformerRegistry,
    UpsertSqlBuilder sqlBuilder,
    ILogger<PostgresGenericApplier> logger)
    : ISyncBatchApplier
{
    private const string CheckpointTable = "sync_meta.checkpoint";

    async Task<SyncRunResult> ISyncBatchApplier.ApplyBatchAsync(
        TableSyncConfig config,
        ChangeBatch batch,
        CancellationToken ct)
    {
        var rule = ruleProvider.Get(config.SourceTable);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            var upsertSql = sqlBuilder.BuildUpsert(rule);
            var lifecycleSql = sqlBuilder.BuildLifecycleByPrimaryKey(rule);
            var hasActiveFlag = sqlBuilder.HasActiveFlag(rule);
            var rowsUpserted = 0;
            var rowsDeactivated = 0;
            var rowsDeleted = 0;

            foreach (var row in batch.Rows)
            {
                if (row.Operation == "D" || row.CurrentValues is null)
                {
                    var affectedRows = await conn.ExecuteAsync(
                        lifecycleSql,
                        BuildPrimaryKeyParameters(rule, row.PrimaryKey),
                        transaction: tx);
                    if (hasActiveFlag)
                        rowsDeactivated += affectedRows;
                    else
                        rowsDeleted += affectedRows;
                    continue;
                }

                var targetValues = BuildTargetValues(rule, row.CurrentValues);
                if (IsActive(rule, row.CurrentValues))
                {
                    await conn.ExecuteAsync(upsertSql, ToDynamicParameters(targetValues), transaction: tx);
                    rowsUpserted++;
                }
                else
                {
                    var affectedRows = await conn.ExecuteAsync(
                        lifecycleSql,
                        BuildPrimaryKeyParameters(rule, targetValues),
                        transaction: tx);
                    if (hasActiveFlag)
                        rowsDeactivated += affectedRows;
                    else
                        rowsDeleted += affectedRows;
                }
            }

            var affected = await conn.ExecuteAsync(
                $@"UPDATE {CheckpointTable}
                   SET last_sync_version = @upperWatermark,
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
                    upperWatermark = batch.UpperWatermark,
                    syncStatus = SyncStatus.CheckpointState.Ready,
                    sourceTable = config.SourceTable,
                    previousCheckpoint = batch.PreviousCheckpoint
                },
                transaction: tx);

            if (affected == 0)
            {
                await tx.RollbackAsync(ct);
                logger.LogWarning(
                    "Concurrent checkpoint modification detected for {SourceTable}. Rolling back.",
                    config.SourceTable);
                return new SyncRunResult
                {
                    Outcome = SyncStatus.Outcome.RequiresFullResync,
                    ErrorCode = "CONCURRENT_CHECKPOINT",
                    ErrorMessage = $"Checkpoint {batch.PreviousCheckpoint} was already advanced by another worker.",
                    RowsRead = batch.Rows.Count,
                    CheckpointBefore = batch.PreviousCheckpoint
                };
            }

            await tx.CommitAsync(ct);

            return new SyncRunResult
            {
                Outcome = SyncStatus.Outcome.Succeeded,
                RowsRead = batch.Rows.Count,
                RowsUpserted = rowsUpserted,
                RowsDeactivated = rowsDeactivated,
                RowsDeleted = rowsDeleted,
                CheckpointBefore = batch.PreviousCheckpoint,
                CheckpointAfter = batch.UpperWatermark
            };
        }
        catch (PostgresException ex) when (IsTransient(ex))
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    async Task<SyncRunResult> ISyncBatchApplier.ApplyBootstrapAsync(
        TableSyncConfig config,
        BootstrapSnapshot snapshot,
        CancellationToken ct)
    {
        var rule = ruleProvider.Get(config.SourceTable);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            var upsertSql = sqlBuilder.BuildUpsert(rule);
            var targetPrimaryKey = GetSingleTargetPrimaryKeyColumn(rule);
            var snapshotPrimaryKeys = new List<object?>(snapshot.Rows.Count);
            var upsertedCount = 0;
            var inactiveLifecycleCount = 0;
            foreach (var row in snapshot.Rows)
            {
                var values = BuildTargetValues(rule, row);
                if (IsActive(rule, row))
                {
                    snapshotPrimaryKeys.Add(values[targetPrimaryKey.TargetColumn]);
                    await conn.ExecuteAsync(upsertSql, ToDynamicParameters(values), transaction: tx);
                    upsertedCount++;
                }
                else
                {
                    inactiveLifecycleCount += await conn.ExecuteAsync(
                        sqlBuilder.BuildLifecycleByPrimaryKey(rule),
                        BuildPrimaryKeyParameters(rule, values),
                        transaction: tx);
                }
            }

            var snapshotPks = BuildTypedPrimaryKeyArray(targetPrimaryKey, snapshotPrimaryKeys);
            var hasActiveFlag = sqlBuilder.HasActiveFlag(rule);
            var orphanLifecycleCount = await conn.ExecuteAsync(
                sqlBuilder.BuildLifecycleOrphans(rule, "sourceSystem"),
                new
                {
                    sourceSystem = rule.OwnershipScope,
                    snapshotPks
                },
                transaction: tx);

            await conn.ExecuteAsync(
                $@"INSERT INTO {CheckpointTable}
                       (source_table, last_sync_version, sync_status, last_attempt_at, last_success_at,
                        consecutive_failure_count, last_error_code, last_error_message)
                   VALUES (@sourceTable, @baselineVersion, @syncStatus, NOW(), NOW(),
                           0, NULL, NULL)
                   ON CONFLICT (source_table)
                   DO UPDATE SET last_sync_version = EXCLUDED.last_sync_version,
                                 sync_status = EXCLUDED.sync_status,
                                 last_attempt_at = NOW(),
                                 last_success_at = NOW(),
                                 consecutive_failure_count = 0,
                                 last_error_code = NULL,
                                 last_error_message = NULL",
                new
                {
                    baselineVersion = snapshot.BaselineVersion,
                    syncStatus = SyncStatus.CheckpointState.Ready,
                    sourceTable = config.SourceTable
                },
                transaction: tx);

            await tx.CommitAsync(ct);

            return new SyncRunResult
            {
                Outcome = SyncStatus.Outcome.Succeeded,
                RowsRead = snapshot.Rows.Count,
                RowsUpserted = upsertedCount,
                RowsDeactivated = hasActiveFlag ? orphanLifecycleCount + inactiveLifecycleCount : 0,
                RowsDeleted = hasActiveFlag ? 0 : orphanLifecycleCount + inactiveLifecycleCount,
                CheckpointAfter = snapshot.BaselineVersion
            };
        }
        catch (PostgresException ex) when (IsTransient(ex))
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private Dictionary<string, object?> BuildTargetValues(TableMappingRule rule, GenericSourceRow sourceRow)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in rule.Columns)
        {
            PostgresTypeMap.EnsureSupported(column.TargetType);
            values[column.TargetColumn] = ResolveColumnValue(rule, column, sourceRow);
        }

        values["source_system"] = rule.OwnershipScope;
        return values;
    }

    private object? ResolveColumnValue(TableMappingRule rule, ColumnMapping column, GenericSourceRow sourceRow)
    {
        if (column.IsActiveFlag)
            return IsActive(rule, sourceRow);
        if (!string.IsNullOrWhiteSpace(column.Transform))
            return transformerRegistry.Resolve(column.Transform).Transform(sourceRow.Values);
        if (!string.IsNullOrWhiteSpace(column.SourceColumn))
            return sourceRow.GetValueOrDefault(column.SourceColumn);

        return sourceRow.GetValueOrDefault(column.TargetColumn);
    }

    private static bool IsActive(TableMappingRule rule, GenericSourceRow sourceRow)
    {
        if (rule.Source.ActivePredicate.Count == 0)
            return true;

        return rule.Source.ActivePredicate.All(predicate => Evaluate(predicate, sourceRow));
    }

    private static bool Evaluate(ColumnPredicate predicate, GenericSourceRow sourceRow)
    {
        var actual = sourceRow.GetValueOrDefault(predicate.Column);
        return predicate.Operator switch
        {
            PredicateOperator.Eq => Equals(actual, predicate.Value),
            PredicateOperator.Neq => !Equals(actual, predicate.Value),
            PredicateOperator.In => AsEnumerable(predicate.Value).Contains(actual),
            PredicateOperator.NotIn => !AsEnumerable(predicate.Value).Contains(actual),
            PredicateOperator.IsNull => actual is null,
            PredicateOperator.IsNotNull => actual is not null,
            PredicateOperator.Gt => Compare(actual, predicate.Value) > 0,
            PredicateOperator.Gte => Compare(actual, predicate.Value) >= 0,
            PredicateOperator.Lt => Compare(actual, predicate.Value) < 0,
            PredicateOperator.Lte => Compare(actual, predicate.Value) <= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(predicate.Operator), predicate.Operator, "Unsupported predicate operator.")
        };
    }

    private static IEnumerable<object?> AsEnumerable(object? value)
        => value is System.Collections.IEnumerable enumerable and not string
            ? enumerable.Cast<object?>()
            : [];

    private static int Compare(object? actual, object? expected)
    {
        if (actual is null || expected is null)
            return actual is null && expected is null ? 0 : actual is null ? -1 : 1;
        if (actual is IComparable comparable)
            return comparable.CompareTo(Convert.ChangeType(expected, actual.GetType()));

        throw new InvalidOperationException($"Value '{actual}' is not comparable.");
    }

    private static DynamicParameters BuildPrimaryKeyParameters(TableMappingRule rule, IReadOnlyList<object?> primaryKey)
    {
        if (primaryKey.Count != rule.Target.PrimaryKey.Count)
            throw new InvalidOperationException($"Primary key value count does not match rule '{rule.RuleName}'.");

        var parameters = new DynamicParameters();
        for (var i = 0; i < rule.Target.PrimaryKey.Count; i++)
        {
            parameters.Add(rule.Target.PrimaryKey[i], primaryKey[i]);
        }

        return parameters;
    }

    private static DynamicParameters BuildPrimaryKeyParameters(
        TableMappingRule rule,
        IReadOnlyDictionary<string, object?> targetValues)
    {
        var parameters = new DynamicParameters();
        foreach (var pk in rule.Target.PrimaryKey)
        {
            parameters.Add(pk, targetValues[pk]);
        }

        return parameters;
    }

    private static DynamicParameters ToDynamicParameters(IReadOnlyDictionary<string, object?> values)
    {
        var parameters = new DynamicParameters();
        foreach (var (key, value) in values)
        {
            parameters.Add(key, value);
        }

        return parameters;
    }

    private static ColumnMapping GetSingleTargetPrimaryKeyColumn(TableMappingRule rule)
    {
        if (rule.Target.PrimaryKey.Count != 1)
            throw new NotSupportedException("Bootstrap orphan lifecycle currently supports a single-column target primary key.");

        var primaryKey = rule.Target.PrimaryKey[0];
        return rule.Columns.Single(c =>
            c.IsPrimaryKey && string.Equals(c.TargetColumn, primaryKey, StringComparison.OrdinalIgnoreCase));
    }

    private static object BuildTypedPrimaryKeyArray(ColumnMapping primaryKey, IReadOnlyList<object?> values)
        => primaryKey.TargetType switch
        {
            "text" => values.Select(ConvertToString).ToArray(),
            "integer" => values.Select(ConvertToInt32).ToArray(),
            "bigint" => values.Select(ConvertToInt64).ToArray(),
            "boolean" => values.Select(ConvertToBoolean).ToArray(),
            "numeric" => values.Select(ConvertToDecimal).ToArray(),
            "date" => values.Select(ConvertToDateOnly).ToArray(),
            "timestamptz" => values.Select(ConvertToDateTimeOffset).ToArray(),
            _ => throw new NotSupportedException(
                $"Target primary key type '{primaryKey.TargetType}' is not supported for bootstrap orphan lifecycle.")
        };

    private static string ConvertToString(object? value)
        => value?.ToString()
            ?? throw new InvalidOperationException("Target primary key value cannot be null.");

    private static int ConvertToInt32(object? value)
        => value is null
            ? throw new InvalidOperationException("Target primary key value cannot be null.")
            : Convert.ToInt32(value);

    private static long ConvertToInt64(object? value)
        => value is null
            ? throw new InvalidOperationException("Target primary key value cannot be null.")
            : Convert.ToInt64(value);

    private static bool ConvertToBoolean(object? value)
        => value is null
            ? throw new InvalidOperationException("Target primary key value cannot be null.")
            : Convert.ToBoolean(value);

    private static decimal ConvertToDecimal(object? value)
        => value is null
            ? throw new InvalidOperationException("Target primary key value cannot be null.")
            : Convert.ToDecimal(value);

    private static DateOnly ConvertToDateOnly(object? value)
        => value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => throw new InvalidOperationException("Target primary key value must be a date.")
        };

    private static DateTimeOffset ConvertToDateTimeOffset(object? value)
        => value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime),
            _ => throw new InvalidOperationException("Target primary key value must be a timestamp.")
        };

    private static bool IsTransient(PostgresException ex)
        => ex.SqlState is "40001" or "40P01"
            or "57P01" or "57P02" or "57P03" or "57P04"
            || (ex.SqlState.Length == 5 && ex.SqlState.StartsWith("08"));
}
