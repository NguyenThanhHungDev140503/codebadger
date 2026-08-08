namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;

public sealed class PostgresBootstrapMonitorQueryService(
    string connectionString,
    IBootstrapJobStateChecker jobStateChecker,
    IBootstrapDiagnosticEventStore eventStore) : IBootstrapMonitorQueryService
{
    private const string ParentSelectColumns = """
        p.parent_id AS "ParentId",
        p.rule_name AS "RuleName",
        p.status AS "Status",
        p.staging_table_name AS "StagingTableName",
        p.baseline_version AS "BaselineVersion",
        p.watermark_version AS "WatermarkVersion",
        p.rows_staged AS "RowsStaged",
        p.attempt_count AS "AttemptCount",
        p.created_at AS "CreatedAt",
        p.last_heartbeat_at AS "LastHeartbeatAt",
        p.completed_at AS "CompletedAt",
        p.phase_job_id AS "PhaseJobId",
        p.phase_job_kind AS "PhaseJobKind",
        p.error_code AS "ErrorCode",
        p.error_message AS "ErrorMessage",
        p.cancel_requested_at AS "CancelRequestedAt"
        """;

    private const string ChildSelectColumns = """
        c.child_id AS "ChildId",
        c.sequence AS "Sequence",
        c.after_key AS "AfterKey",
        c.last_key AS "LastKey",
        c.rows_read AS "RowsRead",
        c.status AS "Status",
        c.attempt_count AS "AttemptCount",
        c.created_at AS "CreatedAt",
        c.last_heartbeat_at AS "LastHeartbeatAt",
        c.hangfire_job_id AS "HangfireJobId",
        c.error_code AS "ErrorCode",
        c.error_message AS "ErrorMessage"
        """;

    public async Task<IReadOnlyList<BootstrapMonitorListItemDto>> GetRequestListAsync(
        string? ruleName, string? status, int pageIndex, int pageSize, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var boundedPageSize = Math.Clamp(pageSize, 1, 200);
        var offset = (pageIndex - 1) * boundedPageSize;

        var whereClauses = new List<string> { "r.bootstrap_type = 'scalable'" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(ruleName))
        {
            whereClauses.Add("r.source_table ILIKE @RuleName");
            parameters.Add("RuleName", $"%{ruleName}%");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClauses.Add("r.status = @Status");
            parameters.Add("Status", status);
        }

        parameters.Add("Offset", offset);
        parameters.Add("Limit", boundedPageSize);

        var where = string.Join(" AND ", whereClauses);

        var sql = $"""
            SELECT
                r.request_id AS "RequestId",
                r.source_table AS "RuleName",
                r.status AS "RequestStatus",
                r.bootstrap_type AS "BootstrapType",
                r.requested_at AS "CreatedAt",
                p.status AS "ParentStatus",
                COALESCE(child_counts.total_children, 0) AS "TotalChildren",
                COALESCE(child_counts.completed_children, 0) AS "CompletedChildren",
                COALESCE(child_counts.failed_children, 0) AS "FailedChildren",
                latest_event.event_type AS "LatestEventType",
                latest_event.occurred_at AS "LatestEventAt"
            FROM sync_meta.bootstrap_request r
            LEFT JOIN sync_meta.bootstrap_parent p
                ON r.request_id = p.bootstrap_request_id
            LEFT JOIN LATERAL (
                SELECT
                    COUNT(*) AS total_children,
                    COUNT(*) FILTER (WHERE status = 'completed') AS completed_children,
                    COUNT(*) FILTER (WHERE status = 'failed') AS failed_children
                FROM sync_meta.bootstrap_child
                WHERE parent_id = p.parent_id
            ) child_counts ON TRUE
            LEFT JOIN LATERAL (
                SELECT event_type, occurred_at
                FROM sync_meta.bootstrap_diagnostic_event
                WHERE request_id = r.request_id
                ORDER BY sequence_no DESC
                LIMIT 1
            ) latest_event ON TRUE
            WHERE {where}
            ORDER BY r.requested_at DESC
            OFFSET @Offset
            LIMIT @Limit
            """;

        var results = await conn.QueryAsync<BootstrapMonitorListItemRow>(sql, parameters);

        return results.Select(r => new BootstrapMonitorListItemDto
        {
            RequestId = r.RequestId,
            RuleName = r.RuleName,
            RequestStatus = r.RequestStatus,
            BootstrapType = r.BootstrapType,
            TotalChildren = r.TotalChildren,
            CompletedChildren = r.CompletedChildren,
            FailedChildren = r.FailedChildren,
            Health = DeriveHealth(r),
            LatestEventType = r.LatestEventType,
            LatestEventAt = r.LatestEventAt,
            CreatedAt = r.CreatedAt
        }).AsList();
    }

    public async Task<BootstrapMonitorDetailDto?> GetDetailAsync(Guid requestId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var request = await conn.QuerySingleOrDefaultAsync<BootstrapRequestRow>(
            """
            SELECT
                request_id AS "RequestId",
                source_table AS "SourceTable",
                status AS "Status",
                bootstrap_type AS "BootstrapType",
                requested_at AS "RequestedAt"
            FROM sync_meta.bootstrap_request
            WHERE request_id = @RequestId
            """,
            new { RequestId = requestId });

        if (request is null)
            return null;

        var parent = await conn.QuerySingleOrDefaultAsync<MonitorParentDto>(
            $"""
            SELECT {ParentSelectColumns}
            FROM sync_meta.bootstrap_parent p
            WHERE p.bootstrap_request_id = @RequestId
            """,
            new { RequestId = requestId });

        var children = await conn.QueryAsync<MonitorChildDto>(
            $"""
            SELECT {ChildSelectColumns}
            FROM sync_meta.bootstrap_child c
            JOIN sync_meta.bootstrap_parent p ON c.parent_id = p.parent_id
            WHERE p.bootstrap_request_id = @RequestId
            ORDER BY c.sequence
            """,
            new { RequestId = requestId });

        var events = await eventStore.GetTimelineAsync(requestId, 1, 50, ct);
        var timeline = events.Select(e => new BootstrapDiagnosticEventDto
        {
            EventId = e.EventId,
            OccurredAt = e.OccurredAt,
            EntityType = e.EntityType,
            EventType = e.EventType,
            FromStatus = e.FromStatus,
            ToStatus = e.ToStatus,
            HangfireJobId = e.HangfireJobId,
            FencingTokenHash = e.FencingTokenHash,
            ChildSequence = e.ChildSequence,
            RowsAffected = e.RowsAffected,
            DiagnosticCode = e.DiagnosticCode,
            SanitizedMessage = e.SanitizedMessage,
            InitiatedBy = e.InitiatedBy,
            SequenceNo = e.SequenceNo
        }).ToList();

        if (parent is not null)
        {
            parent = parent with
            {
                HangfireJobState = DeriveParentHangfireState(parent),
                CanReconcile = ComputeCanReconcile(parent, children),
                CanCancel = ComputeCanCancel(parent)
            };
        }

        var childList = children.Select(c => c with
        {
            HangfireJobState = DeriveChildHangfireState(c),
            CanRetry = ComputeCanRetry(c)
        }).ToList();

        return new BootstrapMonitorDetailDto
        {
            RequestId = request.RequestId,
            RuleName = request.SourceTable,
            RequestStatus = request.Status,
            BootstrapType = request.BootstrapType,
            CreatedAt = request.RequestedAt,
            Parent = parent,
            Children = childList,
            Timeline = timeline
        };
    }

    public async Task<IReadOnlyList<BootstrapDiagnosticEventDto>> GetTimelineAsync(
        Guid requestId, int pageIndex, int pageSize, CancellationToken ct)
    {
        var events = await eventStore.GetTimelineAsync(requestId, pageIndex, pageSize, ct);

        return events.Select(e => new BootstrapDiagnosticEventDto
        {
            EventId = e.EventId,
            OccurredAt = e.OccurredAt,
            EntityType = e.EntityType,
            EventType = e.EventType,
            FromStatus = e.FromStatus,
            ToStatus = e.ToStatus,
            HangfireJobId = e.HangfireJobId,
            FencingTokenHash = e.FencingTokenHash,
            ChildSequence = e.ChildSequence,
            RowsAffected = e.RowsAffected,
            DiagnosticCode = e.DiagnosticCode,
            SanitizedMessage = e.SanitizedMessage,
            InitiatedBy = e.InitiatedBy,
            SequenceNo = e.SequenceNo
        }).AsList();
    }

    private static string DeriveHealth(BootstrapMonitorListItemRow row)
    {
        if (row.ParentStatus is "completed")
            return "Healthy";

        if (row.ParentStatus is "failed" or "cancelled" or "expired")
            return "Failed";

        if (row.ParentStatus is "running" or "catching_up" or "publishing")
        {
            if (row.TotalChildren > 0 && row.CompletedChildren == row.TotalChildren && row.FailedChildren == 0)
                return "Finalizing";
            return "Running";
        }

        if (row.ParentStatus is "pending_enqueue")
            return "Starting";

        if (row.ParentStatus is "cancel_requested" or "recovery_pending")
            return "Stopping";

        if (row.RequestStatus is "completed")
            return "Healthy";

        if (row.RequestStatus is "failed")
            return row.FailedChildren > 0 ? "Failed" : "Unhealthy";

        if (row.RequestStatus is "running" or "queued" or "pending_enqueue")
            return "Running";

        return "Unknown";
    }

    private string DeriveParentHangfireState(MonitorParentDto parent) =>
        jobStateChecker.Probe(parent.PhaseJobId).Kind.ToString();

    private string DeriveChildHangfireState(MonitorChildDto child) =>
        jobStateChecker.Probe(child.HangfireJobId).Kind.ToString();

    private static bool ComputeCanReconcile(MonitorParentDto? parent, IEnumerable<MonitorChildDto> children)
    {
        if (parent is null)
            return false;

        if (parent.Status is BootstrapParentStatus.Failed or BootstrapParentStatus.RecoveryPending
            or BootstrapParentStatus.PendingEnqueue or BootstrapParentStatus.Running
            or BootstrapParentStatus.CatchingUp or BootstrapParentStatus.Publishing
            or BootstrapParentStatus.CancelRequested)
            return true;

        foreach (var child in children)
        {
            if (child.Status is BootstrapChildStatus.Failed or BootstrapChildStatus.Queued
                or BootstrapChildStatus.Running)
                return true;
        }

        return false;
    }

    private static bool ComputeCanCancel(MonitorParentDto? parent)
    {
        if (parent is null)
            return false;

        return parent.Status is BootstrapParentStatus.Running
            or BootstrapParentStatus.CatchingUp
            or BootstrapParentStatus.Publishing;
    }

    private static bool ComputeCanRetry(MonitorChildDto child)
    {
        return child.Status is BootstrapChildStatus.Failed;
    }

    private sealed record BootstrapRequestRow
    {
        public Guid RequestId { get; init; }
        public string SourceTable { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? BootstrapType { get; init; }
        public DateTime RequestedAt { get; init; }
    }

    private sealed record BootstrapMonitorListItemRow
    {
        public Guid RequestId { get; init; }
        public string RuleName { get; init; } = string.Empty;
        public string RequestStatus { get; init; } = string.Empty;
        public string? BootstrapType { get; init; }
        public string? ParentStatus { get; init; }
        public int TotalChildren { get; init; }
        public int CompletedChildren { get; init; }
        public int FailedChildren { get; init; }
        public string? LatestEventType { get; init; }
        public DateTime? LatestEventAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
