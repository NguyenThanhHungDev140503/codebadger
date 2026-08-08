namespace Infrastructure.CentralDbSync;

using System.Data;
using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Queries;
using Dapper;
using Npgsql;

public sealed class PostgresCentralDbSyncQueryService(
    string connectionString,
    IMappingRuleProvider ruleProvider)
    : ICentralDbSyncQueryService
{
    private static readonly string[] FailureOutcomes = ["failed", "requires_full_resync"];
    private static readonly string[] SuccessOutcomes = ["succeeded", "no_changes"];
    private static readonly TimeSpan HotMaxAllowedLag = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ColdMaxAllowedLag = TimeSpan.FromHours(1);

    public async Task<PaginatedResponse<BootstrapJobListItemDto>> GetBootstrapJobsAsync(
        GetBootstrapJobsQuery query, CancellationToken ct)
    {
        var ruleName = string.IsNullOrWhiteSpace(query.RuleName) ? null : query.RuleName.Trim();
        var status = NormalizeBootstrapStatus(query.Status);

        const string countSql = @"
            SELECT COUNT(*)
            FROM sync_meta.bootstrap_request
            WHERE (@RuleName IS NULL OR source_table = @RuleName)
              AND (@Status IS NULL OR
                   CASE
                       WHEN status IN ('pending_enqueue', 'queued', 'waiting_for_lock') THEN 'pending'
                       WHEN status = 'running' THEN 'running'
                       WHEN status = 'completed' THEN 'success'
                       WHEN status = 'failed' THEN 'failed'
                       ELSE status
                   END = @Status);";

        const string itemsSql = @"
            SELECT request_id AS RequestId,
                   source_table AS RuleName,
                   CASE
                       WHEN status IN ('pending_enqueue', 'queued', 'waiting_for_lock') THEN 'Pending'
                       WHEN status = 'running' THEN 'Running'
                       WHEN status = 'completed' THEN 'Success'
                       WHEN status = 'failed' THEN 'Failed'
                       ELSE status
                   END AS Status,
                   started_at AS StartedAt,
                   finished_at AS FinishedAt,
                   error_message AS ErrorMessage
            FROM sync_meta.bootstrap_request
            WHERE (@RuleName IS NULL OR source_table = @RuleName)
              AND (@Status IS NULL OR
                   CASE
                       WHEN status IN ('pending_enqueue', 'queued', 'waiting_for_lock') THEN 'pending'
                       WHEN status = 'running' THEN 'running'
                       WHEN status = 'completed' THEN 'success'
                       WHEN status = 'failed' THEN 'failed'
                       ELSE status
                   END = @Status)
            ORDER BY requested_at DESC, request_id DESC
            OFFSET @Offset LIMIT @PageSize;";

        var offset = (query.PageIndex - 1) * query.PageSize;
        var parameters = new
        {
            RuleName = ruleName,
            Status = status,
            Offset = offset,
            PageSize = query.PageSize
        };

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, transaction, cancellationToken: ct));

        var items = (await connection.QueryAsync<BootstrapJobListItemDto>(
            new CommandDefinition(itemsSql, parameters, transaction, cancellationToken: ct))).ToList();

        await transaction.CommitAsync(ct);

        return new PaginatedResponse<BootstrapJobListItemDto>(
            items, totalCount, query.PageIndex, query.PageSize);
    }

    public async Task<PaginatedResponse<SyncRunLogDto>> GetLogsAsync(
        GetSyncLogsQuery query, CancellationToken ct)
    {
        var ruleName = string.IsNullOrWhiteSpace(query.RuleName) ? null : query.RuleName.Trim();
        var outcome = string.IsNullOrWhiteSpace(query.Outcome) ? null : query.Outcome.Trim().ToLowerInvariant();
        var from = query.From;
        var to = query.To;

        const string countSql = @"
            SELECT COUNT(*)
            FROM sync_meta.sync_run_log
            WHERE (@RuleName IS NULL OR source_table = @RuleName)
              AND (@Outcome IS NULL OR outcome = @Outcome)
              AND (CAST(@From AS timestamptz) IS NULL OR started_at >= CAST(@From AS timestamptz))
              AND (CAST(@To AS timestamptz) IS NULL OR started_at <= CAST(@To AS timestamptz));";

        const string itemsSql = @"
            SELECT id AS Id,
                   source_table AS RuleName,
                   run_id AS RunId,
                   mode AS Mode,
                   outcome AS Outcome,
                   rows_read AS RowsRead,
                   rows_upserted AS RowsUpserted,
                   rows_deactivated AS RowsDeactivated,
                   rows_deleted AS RowsDeleted,
                   checkpoint_before AS CheckpointBefore,
                   checkpoint_after AS CheckpointAfter,
                   started_at AS StartedAt,
                   finished_at AS FinishedAt,
                   duration_ms::bigint AS DurationMs,
                   error_code AS ErrorCode,
                   error_message AS ErrorMessage
            FROM sync_meta.sync_run_log
            WHERE (@RuleName IS NULL OR source_table = @RuleName)
              AND (@Outcome IS NULL OR outcome = @Outcome)
              AND (CAST(@From AS timestamptz) IS NULL OR started_at >= CAST(@From AS timestamptz))
              AND (CAST(@To AS timestamptz) IS NULL OR started_at <= CAST(@To AS timestamptz))
            ORDER BY started_at DESC, id DESC
            OFFSET @Offset LIMIT @PageSize;";

        var offset = (query.PageIndex - 1) * query.PageSize;
        var parameters = new
        {
            RuleName = ruleName,
            Outcome = outcome,
            From = from,
            To = to,
            Offset = offset,
            PageSize = query.PageSize
        };

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, transaction, cancellationToken: ct));

        var items = (await connection.QueryAsync<SyncRunLogDto>(
            new CommandDefinition(itemsSql, parameters, transaction, cancellationToken: ct))).ToList();

        await transaction.CommitAsync(ct);

        return new PaginatedResponse<SyncRunLogDto>(items, totalCount, query.PageIndex, query.PageSize);
    }

    public async Task<SyncOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        const string overviewSql = @"
            WITH latest_runs AS (
                SELECT DISTINCT ON (source_table)
                       source_table, outcome, started_at, duration_ms, rows_upserted, id
                FROM sync_meta.sync_run_log
                ORDER BY source_table, started_at DESC, id DESC
            ),
            config_with_health AS (
                SELECT cfg.*,
                       CASE
                           WHEN LOWER(cfg.sync_tier) = 'hot' THEN INTERVAL '30 minutes'
                           ELSE INTERVAL '1 hour'
                       END AS effective_max_allowed_lag
                FROM sync_meta.table_sync_config cfg
            )
            SELECT cfg.source_table AS RuleName,
                   cfg.enabled AS IsEnabled,
                   cfg.sync_tier AS SyncTier,
                   COALESCE(c.sync_status, 'never_synced') AS SyncStatus,
                   CASE
                       WHEN cfg.enabled = false THEN 'Disabled'
                       WHEN c.last_success_at IS NULL THEN 'NeverSynced'
                       WHEN c.sync_status = 'requires_full_resync'
                            OR COALESCE(c.consecutive_failure_count, 0) > 0 THEN 'Failed'
                       WHEN CURRENT_TIMESTAMP - c.last_success_at >= cfg.effective_max_allowed_lag THEN 'Degraded'
                       ELSE 'Healthy'
                   END AS HealthStatus,
                   c.last_sync_version AS LastSyncVersion,
                   c.last_attempt_at AS LastAttemptAt,
                   c.last_success_at AS LastSuccessAt,
                   c.last_failure_at AS LastFailureAt,
                   cfg.expected_sync_interval AS ExpectedSyncInterval,
                   cfg.effective_max_allowed_lag AS MaxAllowedLag,
                   CASE
                       WHEN c.last_success_at IS NULL THEN NULL
                       ELSE GREATEST(0, FLOOR(EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - c.last_success_at)) * 1000))::bigint
                   END AS LastSyncLagMs,
                   COALESCE(c.consecutive_failure_count, 0) AS ConsecutiveFailureCount,
                   c.last_error_code AS LastErrorCode,
                   c.last_error_message AS LastErrorMessage,
                   lr.outcome AS LatestRunOutcome,
                   lr.started_at AS LatestRunStartedAt,
                   lr.duration_ms::bigint AS LatestRunDurationMs,
                   lr.rows_upserted AS LatestRunRowsUpserted
            FROM config_with_health cfg
            LEFT JOIN sync_meta.checkpoint c ON c.source_table = cfg.source_table
            LEFT JOIN latest_runs lr ON lr.source_table = cfg.source_table
            ORDER BY cfg.source_table;";

        const string runningBootstrapSql = @"
            SELECT COUNT(*)
            FROM sync_meta.bootstrap_request
            WHERE status = 'running';";

        const string errorsLast24hSql = @"
            SELECT COUNT(*)
            FROM sync_meta.sync_run_log
            WHERE outcome = ANY(@FailureOutcomes)
              AND started_at >= NOW() - INTERVAL '24 hours';";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        var overviewList = (await connection.QueryAsync<TableSyncOverviewDto>(
                new CommandDefinition(overviewSql, cancellationToken: ct)))
            .Select(ApplyRuleHealthPolicy)
            .ToList();

        var runningBootstrapJobs = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(runningBootstrapSql, cancellationToken: ct));

        var errorsLast24h = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                errorsLast24hSql,
                new { FailureOutcomes },
                cancellationToken: ct));

        return new SyncOverviewDto(
            overviewList,
            runningBootstrapJobs,
            errorsLast24h);
    }

    public async Task<IReadOnlyList<MonitoringHistoryPointDto>> GetMonitoringHistoryAsync(
        GetMonitoringHistoryQuery query, CancellationToken ct)
    {
        var (from, to) = ResolveWindow(query.From, query.To);
        var bucketSeconds = query.BucketMinutes * 60;

        const string sql = @"
            WITH filtered AS (
                SELECT started_at,
                       outcome,
                       rows_upserted,
                       duration_ms
                FROM sync_meta.sync_run_log
                WHERE started_at >= @From
                  AND started_at <= @To
            ),
            bucketed AS (
                SELECT to_timestamp(
                           floor(extract(epoch FROM started_at) / @BucketSeconds) * @BucketSeconds
                       ) AS bucket_at,
                       outcome,
                       rows_upserted,
                       duration_ms
                FROM filtered
            )
            SELECT bucket_at AS Timestamp,
                   AVG(duration_ms)::bigint AS SyncLagMs,
                   COALESCE(SUM(rows_upserted), 0)::int AS RowsUpserted,
                   COUNT(*) FILTER (WHERE outcome = ANY(@SuccessOutcomes))::int AS SuccessCount,
                   COUNT(*) FILTER (WHERE outcome = ANY(@FailureOutcomes))::int AS FailureCount
            FROM bucketed
            GROUP BY bucket_at
            ORDER BY bucket_at;";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        var items = await connection.QueryAsync<MonitoringHistoryPointDto>(
            new CommandDefinition(
                sql,
                new
                {
                    From = from,
                    To = to,
                    BucketSeconds = bucketSeconds,
                    SuccessOutcomes,
                    FailureOutcomes
                },
                cancellationToken: ct));

        return items.ToList();
    }

    public async Task<MonitoringStatsDto> GetMonitoringStatsAsync(
        GetMonitoringStatsQuery query, CancellationToken ct)
    {
        var (from, to) = ResolveWindow(query.From, query.To);

        const string sql = @"
            WITH counts AS (
                SELECT COUNT(*)::numeric AS total_count,
                       COUNT(*) FILTER (WHERE outcome = ANY(@SuccessOutcomes))::numeric AS success_count,
                       COUNT(*) FILTER (WHERE outcome = ANY(@FailureOutcomes))::numeric AS failure_count,
                       AVG(duration_ms)::bigint AS avg_lag_time_ms
                FROM sync_meta.sync_run_log
                WHERE started_at >= @From
                  AND started_at <= @To
            )
            SELECT CASE
                       WHEN total_count = 0 THEN 0
                       ELSE ROUND(success_count * 100 / total_count, 2)
                   END AS SuccessRate,
                   CASE
                       WHEN total_count = 0 THEN 0
                       ELSE ROUND(failure_count * 100 / total_count, 2)
                   END AS FailureRate,
                   avg_lag_time_ms AS AvgLagTimeMs
            FROM counts;";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        return await connection.QuerySingleAsync<MonitoringStatsDto>(
            new CommandDefinition(
                sql,
                new
                {
                    From = from,
                    To = to,
                    SuccessOutcomes,
                    FailureOutcomes
                },
                cancellationToken: ct));
    }

    private static string? NormalizeBootstrapStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim().ToLowerInvariant();

    private static (DateTime From, DateTime To) ResolveWindow(DateTime? from, DateTime? to)
    {
        var resolvedTo = to ?? DateTime.UtcNow;
        var resolvedFrom = from ?? resolvedTo.AddHours(-24);
        return (resolvedFrom, resolvedTo);
    }

    private TableSyncOverviewDto ApplyRuleHealthPolicy(TableSyncOverviewDto row)
    {
        var syncTier = ruleProvider.TryGet(row.RuleName, out var rule)
            ? rule.SyncTier
            : row.SyncTier;
        var maxAllowedLag = GetMaxAllowedLag(syncTier);

        return row with
        {
            SyncTier = syncTier,
            MaxAllowedLag = maxAllowedLag,
            HealthStatus = ResolveHealthStatus(row, maxAllowedLag)
        };
    }

    private static string ResolveHealthStatus(TableSyncOverviewDto row, TimeSpan maxAllowedLag)
    {
        if (!row.IsEnabled)
            return "Disabled";
        if (row.LastSuccessAt is null)
            return "NeverSynced";
        if (string.Equals(row.SyncStatus, "requires_full_resync", StringComparison.OrdinalIgnoreCase)
            || row.ConsecutiveFailureCount > 0)
            return "Failed";
        if (DateTime.UtcNow - row.LastSuccessAt.Value >= maxAllowedLag)
            return "Degraded";

        return "Healthy";
    }

    private static TimeSpan GetMaxAllowedLag(string syncTier)
        => string.Equals(syncTier, "Hot", StringComparison.OrdinalIgnoreCase)
            ? HotMaxAllowedLag
            : ColdMaxAllowedLag;
}
