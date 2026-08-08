namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;
using System.Data.Common;

/// <summary>
/// PostgreSQL implementation of <see cref="IBootstrapCtDispatchStore"/>.
/// </summary>
public sealed class PostgresBootstrapCtDispatchStore(string connectionString) : IBootstrapCtDispatchStore
{
    private const string SelectColumns = """
        dispatch_id AS "DispatchId",
        rule_name AS "RuleName",
        parent_id AS "ParentId",
        watermark AS "Watermark",
        status AS "Status",
        attempt_count AS "AttemptCount",
        created_at AS "CreatedAt",
        dispatch_lease_until AS "DispatchLeaseUntil",
        dispatch_lease_token AS "DispatchLeaseToken",
        dispatched_at AS "DispatchedAt",
        hangfire_job_id AS "HangfireJobId",
        last_error AS "LastError"
        """;

    public async Task<Guid> CreateInTransactionAsync(
        DbConnection connection, DbTransaction transaction,
        string ruleName, Guid parentId, long watermark, CancellationToken ct)
    {
        var dispatchId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var conn = (NpgsqlConnection)connection;
        await conn.ExecuteAsync("""
            INSERT INTO sync_meta.bootstrap_ct_dispatch
                (dispatch_id, rule_name, parent_id, watermark, status, attempt_count, created_at)
            VALUES
                (@DispatchId, @RuleName, @ParentId, @Watermark, @Status, 0, @CreatedAt)
            """, new
        {
            DispatchId = dispatchId,
            RuleName = ruleName,
            ParentId = parentId,
            Watermark = watermark,
            Status = "pending_dispatch",
            CreatedAt = now
        }, transaction: transaction);

        return dispatchId;
    }

    public async Task<IReadOnlyList<BootstrapCtDispatch>> GetDispatchCandidatesAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var results = await conn.QueryAsync<BootstrapCtDispatch>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_ct_dispatch
            WHERE status = 'pending_dispatch'
               OR (status = 'dispatching' AND dispatch_lease_until < @Now)
            """,
            new { Now = nowUtc });

        return results.AsList();
    }

    public async Task<Guid?> TryClaimForDispatchAsync(
        Guid dispatchId, DateTime leaseUntilUtc, CancellationToken ct)
    {
        var leaseToken = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_ct_dispatch
            SET status = 'dispatching',
                attempt_count = attempt_count + 1,
                dispatch_lease_until = @LeaseUntil,
                dispatch_lease_token = @LeaseToken,
                last_error = NULL
            WHERE dispatch_id = @DispatchId
              AND (
                  status = 'pending_dispatch'
                  OR (status = 'dispatching' AND dispatch_lease_until < @Now)
              )
            """, new
        {
            DispatchId = dispatchId,
            LeaseUntil = leaseUntilUtc,
            LeaseToken = leaseToken,
            Now = DateTime.UtcNow
        });

        return rows > 0 ? leaseToken : null;
    }

    public async Task MarkDispatchedAsync(
        Guid dispatchId, Guid dispatchLeaseToken, string hangfireJobId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_ct_dispatch
            SET status = 'dispatched',
                hangfire_job_id = @HangfireJobId,
                dispatched_at = NOW(),
                dispatch_lease_until = NULL
            WHERE dispatch_id = @DispatchId
              AND status = 'dispatching'
              AND dispatch_lease_token = @DispatchLeaseToken
            """, new
        {
            DispatchId = dispatchId,
            DispatchLeaseToken = dispatchLeaseToken,
            HangfireJobId = hangfireJobId
        });
    }

    public async Task ReleaseForRetryAsync(
        Guid dispatchId, Guid dispatchLeaseToken, string safeError, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_ct_dispatch
            SET status = 'pending_dispatch',
                dispatch_lease_until = NULL,
                last_error = @LastError
            WHERE dispatch_id = @DispatchId
              AND status = 'dispatching'
              AND dispatch_lease_token = @DispatchLeaseToken
            """, new
        {
            DispatchId = dispatchId,
            DispatchLeaseToken = dispatchLeaseToken,
            LastError = safeError
        });
    }
}
