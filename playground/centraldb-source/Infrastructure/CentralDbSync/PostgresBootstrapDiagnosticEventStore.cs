namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;

public sealed class PostgresBootstrapDiagnosticEventStore(string connectionString) : IBootstrapDiagnosticEventStore
{
    private const string SelectColumns = """
        event_id AS "EventId",
        occurred_at AS "OccurredAt",
        request_id AS "RequestId",
        parent_id AS "ParentId",
        child_id AS "ChildId",
        entity_type AS "EntityType",
        event_type AS "EventType",
        from_status AS "FromStatus",
        to_status AS "ToStatus",
        hangfire_job_id AS "HangfireJobId",
        fencing_token_hash AS "FencingTokenHash",
        child_sequence AS "ChildSequence",
        rows_affected AS "RowsAffected",
        diagnostic_code AS "DiagnosticCode",
        sanitized_message AS "SanitizedMessage",
        initiated_by AS "InitiatedBy",
        sequence_no AS "SequenceNo"
        """;

    public async Task<long> AppendAsync(BootstrapDiagnosticEvent evt, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var finalMessage = BootstrapDiagnosticSanitizer.Sanitize(evt.SanitizedMessage ?? string.Empty);
        var finalCode = evt.DiagnosticCode.Length <= 200 ? evt.DiagnosticCode : evt.DiagnosticCode[..200];

        return await conn.QuerySingleAsync<long>(
            """
            INSERT INTO sync_meta.bootstrap_diagnostic_event
                (event_id, occurred_at, request_id, parent_id, child_id,
                 entity_type, event_type, from_status, to_status,
                 hangfire_job_id, fencing_token_hash, child_sequence,
                 rows_affected, diagnostic_code, sanitized_message, initiated_by)
            VALUES
                (@EventId, @OccurredAt, @RequestId, @ParentId, @ChildId,
                 @EntityType, @EventType, @FromStatus, @ToStatus,
                 @HangfireJobId, @FencingTokenHash, @ChildSequence,
                 @RowsAffected, @DiagnosticCode, @SanitizedMessage, @InitiatedBy)
            RETURNING sequence_no
            """,
            new
            {
                evt.EventId,
                evt.OccurredAt,
                evt.RequestId,
                evt.ParentId,
                evt.ChildId,
                evt.EntityType,
                evt.EventType,
                evt.FromStatus,
                evt.ToStatus,
                evt.HangfireJobId,
                evt.FencingTokenHash,
                evt.ChildSequence,
                evt.RowsAffected,
                DiagnosticCode = finalCode,
                SanitizedMessage = finalMessage,
                evt.InitiatedBy
            });
    }

    public async Task<IReadOnlyList<BootstrapDiagnosticEvent>> GetTimelineAsync(
        Guid requestId, int pageIndex, int pageSize, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var boundedPageSize = Math.Clamp(pageSize, 1, 200);
        var offset = (pageIndex - 1) * boundedPageSize;

        var results = await conn.QueryAsync<BootstrapDiagnosticEvent>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_diagnostic_event
            WHERE request_id = @RequestId
            ORDER BY sequence_no DESC
            OFFSET @Offset
            LIMIT @Limit
            """,
            new { RequestId = requestId, Offset = offset, Limit = boundedPageSize });

        return results.AsList();
    }

    public async Task<long> DeleteBeforeAsync(DateTime cutoffUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var deleted = await conn.ExecuteAsync(
            "DELETE FROM sync_meta.bootstrap_diagnostic_event WHERE occurred_at < @CutoffUtc",
            new { CutoffUtc = cutoffUtc });

        return deleted;
    }
}
