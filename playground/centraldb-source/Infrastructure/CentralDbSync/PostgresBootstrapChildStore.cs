namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;

/// <summary>
/// PostgreSQL implementation of <see cref="IBootstrapChildStore"/>.
/// Only the next uncompleted child (by sequence) can be claimed for a given parent.
/// </summary>
public sealed class PostgresBootstrapChildStore(string connectionString) : IBootstrapChildStore
{
    private const string SelectColumns = """
        child_id AS "ChildId",
        parent_id AS "ParentId",
        sequence AS "Sequence",
        after_key AS "AfterKey",
        last_key AS "LastKey",
        rows_read AS "RowsRead",
        status AS "Status",
        attempt_count AS "AttemptCount",
        reconcile_attempt_count AS "ReconcileAttemptCount",
        schedule_failure_count AS "ScheduleFailureCount",
        next_reconcile_at AS "NextReconcileAt",
        hangfire_job_id AS "HangfireJobId",
        reconcile_claim_token AS "ReconcileClaimToken",
        reconcile_claimed_at AS "ReconcileClaimedAt",
        created_at AS "CreatedAt",
        last_heartbeat_at AS "LastHeartbeatAt",
        error_code AS "ErrorCode",
        error_message AS "ErrorMessage"
        """;

    public async Task<BootstrapChild> CreateNextAsync(
        Guid parentId, string? afterKey, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var childId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var sql = $"""
            INSERT INTO sync_meta.bootstrap_child
                (child_id, created_at, parent_id, sequence, after_key,
                 rows_read, status, attempt_count)
            VALUES (
                @ChildId, @CreatedAt, @ParentId,
                (SELECT COALESCE(MAX(sequence), 0) + 1
                 FROM sync_meta.bootstrap_child
                 WHERE parent_id = @ParentId),
                @AfterKey,
                @RowsRead,
                @Status,
                @AttemptCount)
            RETURNING
                {SelectColumns}
            """;

        return await conn.QuerySingleAsync<BootstrapChild>(sql, new
        {
            ChildId = childId,
            CreatedAt = now,
            ParentId = parentId,
            AfterKey = afterKey,
            RowsRead = 0,
            Status = BootstrapChildStatus.PendingEnqueue,
            AttemptCount = 0
        });
    }

    public async Task<BootstrapNextChildResult> TryCreateNextChildAsync(
        Guid parentId, Guid fencingToken,
        int expectedLatestCompletedSequence,
        string? expectedLatestCompletedLastKey,
        string? afterKey,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // First check if a next child already exists (duplicate run, either
        // PendingEnqueue or Queued with matching after_key).
        var existing = await conn.QueryFirstOrDefaultAsync<BootstrapChild>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_child
            WHERE parent_id = @ParentId
              AND sequence = @ExpectedNextSequence
              AND status IN (@PendingEnqueue, @Queued)
            LIMIT 1
            """,
            new
            {
                ParentId = parentId,
                ExpectedNextSequence = expectedLatestCompletedSequence + 1,
                PendingEnqueue = BootstrapChildStatus.PendingEnqueue,
                Queued = BootstrapChildStatus.Queued
            });

        if (existing is not null)
            return new BootstrapNextChildResult(existing, WasCreated: false);

        // Create only when:
        // - parent fencing token matches
        // - parent is Running
        // - latest completed child has the expected sequence/last_key
        var childId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var created = await conn.QueryFirstOrDefaultAsync<BootstrapChild>(
            $"""
            INSERT INTO sync_meta.bootstrap_child
                (child_id, created_at, parent_id, sequence, after_key,
                 rows_read, status, attempt_count)
            SELECT @ChildId, @CreatedAt, parent.parent_id,
                   @ExpectedNextSequence,
                   @AfterKey,
                   @RowsRead,
                   @Status,
                   @AttemptCount
            FROM sync_meta.bootstrap_parent AS parent
            WHERE parent.parent_id = @ParentId
              AND parent.fencing_token = @FencingToken
              AND parent.status = 'running'
              AND (
                  NOT EXISTS (
                      SELECT 1 FROM sync_meta.bootstrap_child latest
                      WHERE latest.parent_id = @ParentId
                        AND latest.status = 'completed'
                      HAVING MAX(latest.sequence) IS DISTINCT FROM @ExpectedLatestCompletedSequence
                          OR MAX(CASE WHEN latest.sequence = @ExpectedLatestCompletedSequence THEN latest.last_key END)
                             IS DISTINCT FROM @ExpectedLatestCompletedLastKey
                  )
                  OR @ExpectedLatestCompletedSequence = 0
              )
              AND NOT EXISTS (
                  SELECT 1 FROM sync_meta.bootstrap_child dup
                  WHERE dup.parent_id = @ParentId AND dup.sequence = @ExpectedNextSequence
              )
            ON CONFLICT DO NOTHING
            RETURNING
                {SelectColumns}
            """,
            new
            {
                ChildId = childId,
                CreatedAt = now,
                ParentId = parentId,
                FencingToken = fencingToken,
                ExpectedNextSequence = expectedLatestCompletedSequence + 1,
                ExpectedLatestCompletedSequence = expectedLatestCompletedSequence,
                ExpectedLatestCompletedLastKey = expectedLatestCompletedLastKey,
                AfterKey = afterKey,
                RowsRead = 0,
                Status = BootstrapChildStatus.PendingEnqueue,
                AttemptCount = 0
            });

        if (created is not null)
            return new BootstrapNextChildResult(created, WasCreated: true);

        // Race: another reconciler may have created the child concurrently.
        // Return the existing one so the caller can schedule it.
        var raced = await conn.QueryFirstOrDefaultAsync<BootstrapChild>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_child
            WHERE parent_id = @ParentId
              AND sequence = @ExpectedNextSequence
            LIMIT 1
            """,
            new { ParentId = parentId, ExpectedNextSequence = expectedLatestCompletedSequence + 1 });

        return raced is not null
            ? new BootstrapNextChildResult(raced, WasCreated: false)
            : throw new InvalidOperationException(
            $"Failed to create or find next child for parent {parentId} at sequence {expectedLatestCompletedSequence + 1}");
    }

    public async Task<BootstrapChild?> GetAsync(Guid childId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<BootstrapChild>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_child
            WHERE child_id = @ChildId
            """,
            new { ChildId = childId });
    }

    public async Task<IReadOnlyList<BootstrapChild>> GetByParentAsync(
        Guid parentId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var results = await conn.QueryAsync<BootstrapChild>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_child
            WHERE parent_id = @ParentId
            ORDER BY sequence
            """,
            new { ParentId = parentId });

        return results.AsList();
    }

    public async Task<bool> TryClaimAsync(Guid childId, Guid parentId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // Only the next uncompleted child can be claimed: sequence == min sequence
        // of non-terminal children for this parent.
        // PendingEnqueue is claimable alongside Queued: the Hangfire job is enqueued
        // before SetHangfireJobIdAsync promotes the row, so a worker can legitimately
        // pick up the job while the row is still PendingEnqueue.
        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_child
            SET status = @RunningStatus, last_heartbeat_at = NOW()
            WHERE child_id = @ChildId
              AND parent_id = @ParentId
              AND status IN (@PendingStatus, @QueuedStatus)
              AND sequence = (
                  SELECT MIN(sequence)
                  FROM sync_meta.bootstrap_child
                  WHERE parent_id = @ParentId
                    AND status NOT IN (@CompletedStatus, @FailedStatus))
            """, new
        {
            ChildId = childId,
            ParentId = parentId,
            PendingStatus = BootstrapChildStatus.PendingEnqueue,
            QueuedStatus = BootstrapChildStatus.Queued,
            RunningStatus = BootstrapChildStatus.Running,
            CompletedStatus = BootstrapChildStatus.Completed,
            FailedStatus = BootstrapChildStatus.Failed
        });

        return rows > 0;
    }

    public async Task<bool> TryClaimAsync(Guid childId, Guid parentId, Guid fencingToken,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_child AS child
            SET status = 'running', last_heartbeat_at = NOW()
            WHERE child.child_id = @ChildId AND child.parent_id = @ParentId
              AND child.status IN ('pending_enqueue', 'queued')
              AND EXISTS (SELECT 1 FROM sync_meta.bootstrap_parent parent
                          WHERE parent.parent_id = @ParentId
                            AND parent.fencing_token = @FencingToken
                            AND parent.status = 'running')
              AND child.sequence = (SELECT MIN(sequence)
                                    FROM sync_meta.bootstrap_child
                                    WHERE parent_id = @ParentId
                                      AND status NOT IN ('completed', 'failed'))
            """, new { ChildId = childId, ParentId = parentId, FencingToken = fencingToken },
            cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryClaimInitialAsync(Guid childId, Guid parentId, Guid fencingToken,
        string claimToken, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_child AS child
            SET reconcile_claim_token = @ClaimToken, reconcile_claimed_at = NOW()
            WHERE child.child_id = @ChildId AND child.parent_id = @ParentId
              AND child.status = 'pending_enqueue'
              AND EXISTS (SELECT 1 FROM sync_meta.bootstrap_parent parent
                          WHERE parent.parent_id = @ParentId
                            AND parent.fencing_token = @FencingToken
                            AND parent.status = 'running')
            """, new { ChildId = childId, ParentId = parentId, FencingToken = fencingToken,
                ClaimToken = claimToken }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryFinalizeInitialClaimAsync(Guid childId, Guid parentId,
        Guid fencingToken, string claimToken, string actualJobId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_child AS child
            SET status = 'queued', hangfire_job_id = @ActualJobId,
                reconcile_claim_token = NULL, reconcile_claimed_at = NULL
            WHERE child.child_id = @ChildId AND child.parent_id = @ParentId
              AND child.status = 'pending_enqueue'
              AND child.reconcile_claim_token = @ClaimToken
              AND EXISTS (SELECT 1 FROM sync_meta.bootstrap_parent parent
                          WHERE parent.parent_id = @ParentId
                            AND parent.fencing_token = @FencingToken
                            AND parent.status = 'running')
            """, new { ChildId = childId, ParentId = parentId, FencingToken = fencingToken,
                ClaimToken = claimToken, ActualJobId = actualJobId }, cancellationToken: ct));
        return rows == 1;
    }

    public Task<bool> CompleteAsync(Guid childId, Guid parentId,
        string? lastKey, long rowsRead, CancellationToken ct) =>
        throw new NotSupportedException("Use fenced TryCompleteAsync with the parent fencing token.");

    public async Task<bool> TryCompleteAsync(Guid childId, Guid parentId, Guid fencingToken,
        string? lastKey, long rowsRead, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_child AS child
            SET status = 'completed', last_key = @LastKey, rows_read = @RowsRead,
                last_heartbeat_at = NOW()
            WHERE child.child_id = @ChildId AND child.parent_id = @ParentId
              AND child.status = 'running'
              AND EXISTS (SELECT 1 FROM sync_meta.bootstrap_parent parent
                          WHERE parent.parent_id = @ParentId
                            AND parent.fencing_token = @FencingToken
                            AND parent.status = 'running')
            """, new { ChildId = childId, ParentId = parentId, FencingToken = fencingToken,
                LastKey = lastKey, RowsRead = rowsRead }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> MarkFailedAsync(Guid childId, Guid parentId,
        string errorCode, string errorMessage, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_child
            SET status = @FailedStatus,
                error_code = @ErrorCode,
                error_message = @ErrorMessage,
                last_heartbeat_at = NOW()
            WHERE child_id = @ChildId
              AND parent_id = @ParentId
              AND status NOT IN (@CompletedStatus, @FailedStatus)
            """, new
        {
            ChildId = childId,
            ParentId = parentId,
            FailedStatus = BootstrapChildStatus.Failed,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            CompletedStatus = BootstrapChildStatus.Completed
        });

        return rows > 0;
    }

    public async Task<bool> HeartbeatAsync(Guid childId, Guid parentId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_child
            SET last_heartbeat_at = NOW()
            WHERE child_id = @ChildId AND parent_id = @ParentId
            """, new { ChildId = childId, ParentId = parentId });

        return rows > 0;
    }

    public async Task<bool> TryClaimRecoveryAsync(BootstrapChildRecoveryExpectation expectation,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_child AS child
            SET reconcile_claim_token = @ClaimToken, reconcile_claimed_at = NOW()
            WHERE child.child_id = @ChildId AND child.parent_id = @ParentId
              AND child.status = @ExpectedStatus
              AND COALESCE(child.hangfire_job_id, '') = COALESCE(@ExpectedJobId, '')
              AND (child.next_reconcile_at IS NULL OR child.next_reconcile_at <= NOW())
              AND (child.reconcile_claim_token IS NULL OR child.reconcile_claimed_at <= @StaleClaimBeforeUtc)
              AND EXISTS (SELECT 1 FROM sync_meta.bootstrap_parent parent
                          WHERE parent.parent_id = @ParentId AND parent.fencing_token = @FencingToken
                            AND parent.status = 'running')
            """, new
            {
                expectation.ChildId,
                expectation.ParentId,
                expectation.FencingToken,
                expectation.ExpectedStatus,
                ExpectedJobId = expectation.ExpectedJobId,
                ClaimToken = expectation.ClaimToken,
                StaleClaimBeforeUtc = expectation.StaleClaimBeforeUtc
            }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryFinalizeRecoveryAsync(Guid childId, Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken, string actualJobId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        // Recovery for Running children must CAS Running→Queued so the normal
        // flow (TryClaimAsync: PendingEnqueue/Queued→Running) can resume execution.
        var statusSet = expectedStatus == BootstrapChildStatus.Running
            ? ", status = 'queued'"
            : ", status = CASE WHEN child.status = 'pending_enqueue' THEN 'queued' ELSE child.status END";
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            $@"UPDATE sync_meta.bootstrap_child AS child
            SET hangfire_job_id = @ActualJobId{statusSet},
                reconcile_claim_token = NULL, reconcile_claimed_at = NULL,
                reconcile_attempt_count = CASE WHEN @ExpectedStatus <> 'pending_enqueue'
                    THEN reconcile_attempt_count + 1 ELSE reconcile_attempt_count END,
                schedule_failure_count = 0, next_reconcile_at = NULL
            WHERE child.child_id = @ChildId AND child.parent_id = @ParentId
              AND child.status = @ExpectedStatus AND child.reconcile_claim_token = @ClaimToken
              AND EXISTS (SELECT 1 FROM sync_meta.bootstrap_parent parent
                          WHERE parent.parent_id = @ParentId AND parent.fencing_token = @FencingToken
                            AND parent.status = 'running')",
            new { ChildId = childId, ParentId = parentId, FencingToken = fencingToken,
                ExpectedStatus = expectedStatus, ClaimToken = claimToken, ActualJobId = actualJobId }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryRecordRecoverySchedulingFailureAsync(
        BootstrapChildRecoveryExpectation expectation,
        string errorCode,
        string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_child AS child
            SET error_code = @ErrorCode,
                error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                schedule_failure_count = schedule_failure_count + 1,
                next_reconcile_at = NOW() + CASE schedule_failure_count
                    WHEN 0 THEN INTERVAL '1 minute' WHEN 1 THEN INTERVAL '5 minutes'
                    ELSE INTERVAL '15 minutes' END
            WHERE child.child_id = @ChildId
              AND child.parent_id = @ParentId
              AND child.status = @ExpectedStatus
              AND child.reconcile_claim_token = @ClaimToken
              AND child.schedule_failure_count < @MaxFailures
              AND EXISTS (
                  SELECT 1 FROM sync_meta.bootstrap_parent parent
                  WHERE parent.parent_id = @ParentId
                    AND parent.fencing_token = @FencingToken
                    AND parent.status = 'running')
            """, new
        {
            expectation.ChildId,
            expectation.ParentId,
            expectation.FencingToken,
            expectation.ExpectedStatus,
            expectation.ClaimToken,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength,
            MaxFailures = BootstrapRecoverySchedulePolicy.MaxConsecutiveScheduleFailures
        }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<BootstrapChildRetryResult> TryClaimRetryAsync(
        Guid childId, Guid parentId,
        string expectedChildStatus,
        string? expectedChildHangfireJobId,
        string claimToken,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);

        var child = await conn.QueryFirstOrDefaultAsync<BootstrapChild>(
            new CommandDefinition($"""
            WITH child_update AS (
                UPDATE sync_meta.bootstrap_child
                SET status = 'pending_enqueue',
                    error_code = NULL, error_message = NULL,
                    rows_read = 0,
                    attempt_count = attempt_count + 1,
                    reconcile_claim_token = @ClaimToken,
                    reconcile_claimed_at = NOW()
                WHERE child_id = @ChildId
                  AND parent_id = @ParentId
                  AND status = @ExpectedStatus
                  AND COALESCE(hangfire_job_id, '') = COALESCE(@ExpectedHangfireJobId, '')
                  AND (next_reconcile_at IS NULL OR next_reconcile_at <= NOW())
                RETURNING *
            )
            SELECT {SelectColumns}
            FROM child_update
            """, new
            {
                ChildId = childId,
                ParentId = parentId,
                ExpectedStatus = expectedChildStatus,
                ExpectedHangfireJobId = expectedChildHangfireJobId,
                ClaimToken = claimToken
            }, cancellationToken: ct));

        return child is not null
            ? new BootstrapChildRetryResult { Claimed = true, Child = child }
            : new BootstrapChildRetryResult { Claimed = false };
    }

    public Task<bool> SetHangfireJobIdAsync(Guid childId, Guid parentId,
        string hangfireJobId, CancellationToken ct) =>
        throw new NotSupportedException("Use fenced claim finalization with the actual JobId.");
}
