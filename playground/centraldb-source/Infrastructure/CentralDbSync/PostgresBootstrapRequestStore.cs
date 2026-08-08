namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;

public sealed class PostgresBootstrapRequestStore(string connectionString)
    : IBootstrapRequestStore
{
    private const string TableName = "sync_meta.bootstrap_request";

    private const string RequestSelectColumns = @"
                request_id AS RequestId,
                       source_table AS SourceTable,
                       status AS Status,
                       bootstrap_type AS BootstrapType,
                       hangfire_job_id AS HangfireJobId,
                       rows_staged AS RowsStaged,
                       total_rows_expected AS TotalRowsExpected,
                       attempt_count AS AttemptCount,
                       reconcile_attempt_count AS ReconcileAttemptCount,
                       schedule_failure_count AS ScheduleFailureCount,
                       next_reconcile_at AS NextReconcileAt,
                       requested_at AS RequestedAt,
                       updated_at AS UpdatedAt,
                       started_at AS StartedAt,
                       finished_at AS FinishedAt,
                       first_recovery_at AS FirstRecoveryAt,
                       last_recovery_at AS LastRecoveryAt,
                       reconcile_claim_token AS ReconcileClaimToken,
                       reconcile_claimed_at AS ReconcileClaimedAt,
                       error_code AS ErrorCode,
                       error_message AS ErrorMessage";

    public async Task<BootstrapRequestResult> CreateOrGetActiveAsync(
        string sourceTable, CancellationToken ct, string bootstrapType = "in_memory")
    {
        await using var conn = new NpgsqlConnection(connectionString);

        var existing = await conn.QueryFirstOrDefaultAsync<BootstrapRequestRow>(
            new CommandDefinition($@"
                SELECT {RequestSelectColumns}
                FROM {TableName}
                WHERE source_table = @SourceTable
                  AND status IN ('pending_enqueue', 'queued', 'running', 'waiting_for_lock')
                LIMIT 1",
                new { SourceTable = sourceTable }, cancellationToken: ct));

        if (existing is not null)
            return new BootstrapRequestResult(existing.ToModel(), false);

        var requestId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var actualRequestId = await conn.QuerySingleAsync<Guid>(new CommandDefinition($@"
            INSERT INTO {TableName}
                (request_id, source_table, status, bootstrap_type, rows_staged, total_rows_expected,
                 attempt_count, requested_at, updated_at)
            VALUES
                (@RequestId, @SourceTable, 'pending_enqueue', @BootstrapType, 0, NULL, 0, @Now, @Now)
            ON CONFLICT (source_table)
                WHERE status IN ('pending_enqueue', 'queued', 'running', 'waiting_for_lock')
            DO UPDATE SET updated_at = EXCLUDED.updated_at
            RETURNING request_id",
            new { RequestId = requestId, SourceTable = sourceTable, BootstrapType = bootstrapType, Now = now },
            cancellationToken: ct));

        if (actualRequestId == requestId)
        {
            var created = new BootstrapRequest
            {
                RequestId = requestId, SourceTable = sourceTable,
                Status = BootstrapRequestStatus.PendingEnqueue, BootstrapType = bootstrapType,
                RequestedAt = now, UpdatedAt = now
            };
            return new BootstrapRequestResult(created, true);
        }

        var raced = await conn.QueryFirstOrDefaultAsync<BootstrapRequestRow>(
            new CommandDefinition($@"
                SELECT {RequestSelectColumns}
                FROM {TableName}
                WHERE request_id = @ActualRequestId",
                new { ActualRequestId = actualRequestId }, cancellationToken: ct));

        return raced is not null
            ? new BootstrapRequestResult(raced.ToModel(), false)
            : throw new InvalidOperationException(
                $"Failed to create or find active request for table '{sourceTable}'.");
    }

    public async Task<BootstrapRequest?> GetAsync(Guid requestId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<BootstrapRequestRow>(
            new CommandDefinition($@"SELECT {RequestSelectColumns} FROM {TableName} WHERE request_id = @RequestId",
                new { RequestId = requestId }, cancellationToken: ct));
        return row?.ToModel();
    }

    public Task<bool> MarkQueuedAsync(Guid requestId, string hangfireJobId, CancellationToken ct) =>
        throw new NotSupportedException("Use TryMarkQueuedAsync with a claim snapshot.");

    public async Task<bool> TryMarkQueuedAsync(Guid requestId, string expectedStatus,
        string expectedClaimToken, string hangfireJobId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName}
            SET status = 'queued', hangfire_job_id = @HangfireJobId,
                reconcile_claim_token = NULL, reconcile_claimed_at = NULL, updated_at = NOW()
            WHERE request_id = @RequestId AND status = @ExpectedStatus
              AND reconcile_claim_token = @ExpectedClaimToken",
            new { RequestId = requestId, ExpectedStatus = expectedStatus,
                ExpectedClaimToken = expectedClaimToken, HangfireJobId = hangfireJobId },
            cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryMarkRunningAsync(Guid requestId, string expectedStatus,
        string expectedJobId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName}
            SET status = 'running', started_at = COALESCE(started_at, NOW()),
                attempt_count = attempt_count + 1, updated_at = NOW()
            WHERE request_id = @RequestId AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedJobId",
            new { RequestId = requestId, ExpectedStatus = expectedStatus,
                ExpectedJobId = expectedJobId }, cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryMarkWaitingForLockAsync(Guid requestId, string expectedStatus,
        string expectedJobId, string message, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName} SET status = 'waiting_for_lock', error_message = @Message, updated_at = NOW()
            WHERE request_id = @RequestId AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedJobId",
            new { RequestId = requestId, ExpectedStatus = expectedStatus,
                ExpectedJobId = expectedJobId, Message = message }, cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryCompleteAsync(Guid requestId, string expectedStatus,
        string expectedJobId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName} SET status = 'completed', finished_at = NOW(), updated_at = NOW()
            WHERE request_id = @RequestId AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedJobId",
            new { RequestId = requestId, ExpectedStatus = expectedStatus,
                ExpectedJobId = expectedJobId }, cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryFailAsync(Guid requestId, string expectedStatus,
        string expectedJobId, string code, string message, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName} SET status = 'failed', finished_at = NOW(),
                error_code = @Code, error_message = LEFT(@Message, @MaxLength), updated_at = NOW()
                , reconcile_claim_token = NULL, reconcile_claimed_at = NULL
            WHERE request_id = @RequestId AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedJobId",
            new { RequestId = requestId, ExpectedStatus = expectedStatus,
                ExpectedJobId = expectedJobId, Code = code, Message = message,
                MaxLength = BootstrapRecoveryConstants.MaxPersistedErrorLength },
            cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryRecordSchedulingFailureAsync(Guid requestId, string expectedStatus,
        string expectedJobId, string? claimToken, string code, string message, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName}
            SET schedule_failure_count = schedule_failure_count + 1,
                status = CASE WHEN schedule_failure_count + 1 >= @MaxFailures
                              THEN 'failed' ELSE status END,
                finished_at = CASE WHEN schedule_failure_count + 1 >= @MaxFailures
                                   THEN NOW() ELSE finished_at END,
                reconcile_claim_token = CASE WHEN schedule_failure_count + 1 >= @MaxFailures
                                             THEN NULL ELSE reconcile_claim_token END,
                reconcile_claimed_at = CASE WHEN schedule_failure_count + 1 >= @MaxFailures
                                            THEN NULL ELSE reconcile_claimed_at END,
                next_reconcile_at = NOW() + CASE schedule_failure_count
                    WHEN 0 THEN INTERVAL '1 minute' WHEN 1 THEN INTERVAL '5 minutes'
                    ELSE INTERVAL '15 minutes' END,
                error_code = CASE WHEN schedule_failure_count + 1 >= @MaxFailures
                                  THEN 'BootstrapRecoverySchedulingExhausted' ELSE @Code END,
                error_message = LEFT(@Message, @MaxLength), updated_at = NOW()
            WHERE request_id = @RequestId AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedJobId
              AND (@ClaimToken IS NULL OR reconcile_claim_token = @ClaimToken)
              AND schedule_failure_count < @MaxFailures",
            new { RequestId = requestId, ExpectedStatus = expectedStatus,
                ExpectedJobId = expectedJobId, ClaimToken = claimToken, Code = code,
                Message = message, MaxLength = BootstrapRecoveryConstants.MaxPersistedErrorLength,
                MaxFailures = BootstrapRecoverySchedulePolicy.MaxConsecutiveScheduleFailures },
            cancellationToken: ct));
        return changed == 1;
    }

    public Task<bool> TryMarkRunningAsync(Guid requestId, CancellationToken ct) =>
        throw new NotSupportedException("Use snapshot-guarded TryMarkRunningAsync.");

    public Task MarkWaitingForLockAsync(Guid requestId, string message, CancellationToken ct) =>
        throw new NotSupportedException("Use TryMarkWaitingForLockAsync with a snapshot.");

    public Task MarkCompletedAsync(Guid requestId, CancellationToken ct) =>
        throw new NotSupportedException("Use TryCompleteAsync with a snapshot.");

    public Task MarkFailedAsync(Guid requestId, string code, string message, CancellationToken ct) =>
        throw new NotSupportedException("Use TryFailAsync with a snapshot.");

    public async Task<bool> TryClaimSlotAsync(
        BootstrapRecoveryExpectation expectation,
        string claimToken,
        DateTime staleClaimBeforeUtc,
        bool isRecovery,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName}
            SET reconcile_claim_token = @ClaimToken,
                reconcile_claimed_at = NOW(),
                updated_at = NOW()
            WHERE request_id = @RequestId
              AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedHangfireJobId
              AND reconcile_attempt_count = @ExpectedReconcileAttemptCount
              AND (next_reconcile_at IS NULL OR next_reconcile_at <= NOW())
              AND (
                  reconcile_claim_token IS NULL
                  OR reconcile_claimed_at <= @StaleClaimBeforeUtc
              )
              AND (
                  @IsRecovery = FALSE
                  OR reconcile_attempt_count < @MaxRecoveryAttempts
              )",
            new
            {
                expectation.RequestId,
                expectation.ExpectedStatus,
                expectation.ExpectedHangfireJobId,
                expectation.ExpectedReconcileAttemptCount,
                ClaimToken = claimToken,
                StaleClaimBeforeUtc = staleClaimBeforeUtc,
                IsRecovery = isRecovery,
                MaxRecoveryAttempts = BootstrapRecoverySchedulePolicy.MaxSuccessfulRecoveries
            },
            cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryFinalizeClaimAsync(
        BootstrapRecoveryExpectation expectation,
        string claimToken,
        string finalJobId,
        bool isRecovery,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName}
            SET status = 'queued',
                hangfire_job_id = @FinalJobId,
                reconcile_claim_token = NULL,
                reconcile_claimed_at = NULL,
                schedule_failure_count = 0,
                next_reconcile_at = NULL,
                reconcile_attempt_count =
                    CASE WHEN @IsRecovery
                         THEN reconcile_attempt_count + 1
                         ELSE reconcile_attempt_count
                    END,
                first_recovery_at =
                    CASE WHEN @IsRecovery
                         THEN COALESCE(first_recovery_at, NOW())
                         ELSE first_recovery_at
                    END,
                last_recovery_at =
                    CASE WHEN @IsRecovery
                         THEN NOW()
                         ELSE last_recovery_at
                    END,
                error_code = NULL,
                error_message = NULL,
                updated_at = NOW()
            WHERE request_id = @RequestId
              AND reconcile_claim_token = @ClaimToken
              AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedHangfireJobId
              AND reconcile_attempt_count = @ExpectedReconcileAttemptCount
              AND (next_reconcile_at IS NULL OR next_reconcile_at <= NOW())
              AND (
                  @IsRecovery = FALSE
                  OR reconcile_attempt_count < @MaxRecoveryAttempts
              )",
            new
            {
                expectation.RequestId,
                expectation.ExpectedStatus,
                expectation.ExpectedHangfireJobId,
                expectation.ExpectedReconcileAttemptCount,
                ClaimToken = claimToken,
                FinalJobId = finalJobId,
                IsRecovery = isRecovery,
                MaxRecoveryAttempts = BootstrapRecoverySchedulePolicy.MaxSuccessfulRecoveries
            },
            cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryReassignScalableStartJobAsync(
        Guid requestId,
        Guid parentId,
        Guid fencingToken,
        string expectedJobId,
        string? expectedPhaseJobId,
        string replacementJobId,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        // A single statement makes the replacement transfer atomic: a stale
        // parent-start cannot update the request without also transferring the
        // durable parent phase ownership (or vice versa).
        var changed = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET phase_job_id = @ReplacementJobId,
                    phase_job_kind = 'parent_start',
                    phase_claim_token = NULL,
                    phase_claimed_at = NULL,
                    last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = 'pending_enqueue'
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET hangfire_job_id = @ReplacementJobId,
                    updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status IN ('queued', 'running')
                  AND request.hangfire_job_id = @ExpectedJobId
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            RequestId = requestId,
            ParentId = parentId,
            FencingToken = fencingToken,
            ExpectedJobId = expectedJobId,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ReplacementJobId = replacementJobId
        }, transaction, cancellationToken: ct));

        if (changed != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryRecordScalableStartSchedulingFailureAsync(
        BootstrapRecoveryExpectation requestExpectation,
        Guid parentId,
        Guid fencingToken,
        string expectedParentStatus,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        var changed = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET phase_schedule_failure_count = parent.phase_schedule_failure_count + 1,
                    phase_next_reconcile_at = NOW() + CASE parent.phase_schedule_failure_count
                        WHEN 0 THEN INTERVAL '1 minute'
                        WHEN 1 THEN INTERVAL '5 minutes'
                        ELSE INTERVAL '15 minutes' END,
                    status = CASE WHEN parent.phase_schedule_failure_count + 1 >= @MaxFailures
                                  THEN 'failed' ELSE parent.status END,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxLength),
                    last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                  AND parent.phase_schedule_failure_count < @MaxFailures
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET schedule_failure_count = request.schedule_failure_count + 1,
                    next_reconcile_at = NOW() + CASE request.schedule_failure_count
                        WHEN 0 THEN INTERVAL '1 minute'
                        WHEN 1 THEN INTERVAL '5 minutes'
                        ELSE INTERVAL '15 minutes' END,
                    status = CASE WHEN request.schedule_failure_count + 1 >= @MaxFailures
                                  THEN 'failed' ELSE request.status END,
                    finished_at = CASE WHEN request.schedule_failure_count + 1 >= @MaxFailures
                                       THEN NOW() ELSE request.finished_at END,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxLength),
                    updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND request.hangfire_job_id = @ExpectedHangfireJobId
                  AND request.schedule_failure_count < @MaxFailures
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            requestExpectation.RequestId,
            requestExpectation.ExpectedStatus,
            requestExpectation.ExpectedHangfireJobId,
            ParentId = parentId,
            FencingToken = fencingToken,
            ExpectedParentStatus = expectedParentStatus,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxLength = BootstrapRecoveryConstants.MaxPersistedErrorLength,
            MaxFailures = BootstrapRecoverySchedulePolicy.MaxConsecutiveScheduleFailures
        }, transaction, cancellationToken: ct));

        if (changed != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFailClaimAsync(
        Guid requestId, string claimToken, string errorCode, string errorMessage, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName}
            SET status = 'failed', finished_at = NOW(),
                error_code = @ErrorCode, error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                reconcile_claim_token = NULL, reconcile_claimed_at = NULL, updated_at = NOW()
            WHERE request_id = @RequestId AND reconcile_claim_token = @ClaimToken",
            new { RequestId = requestId, ClaimToken = claimToken, ErrorCode = errorCode,
                ErrorMessage = errorMessage, MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength },
            cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryFailScalableRecoveryExhaustedAsync(
        BootstrapRecoveryExpectation expectation, Guid parentId, Guid fencingToken,
        string expectedParentStatus, DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId, string errorCode, string errorMessage, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET status = 'failed', error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.last_heartbeat_at IS NOT DISTINCT FROM @ExpectedLastHeartbeatAt
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'failed', finished_at = NOW(), error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), updated_at = NOW(),
                    reconcile_claim_token = NULL, reconcile_claimed_at = NULL
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            expectation.RequestId, expectation.ExpectedStatus, expectation.ExpectedHangfireJobId,
            expectation.ExpectedReconcileAttemptCount, ParentId = parentId, FencingToken = fencingToken,
            ExpectedParentStatus = expectedParentStatus,
            ExpectedLastHeartbeatAt = (object?)expectedLastHeartbeatAt ?? DBNull.Value,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ErrorCode = errorCode, ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<BootstrapRequest>> GetPendingEnqueueBeforeAsync(
        DateTime cutoffUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.QueryAsync<BootstrapRequestRow>(
            new CommandDefinition($@"
                SELECT {RequestSelectColumns} FROM {TableName}
                WHERE status = 'pending_enqueue' AND requested_at <= @Cutoff",
                new { Cutoff = cutoffUtc }, cancellationToken: ct));
        return rows.Select(r => r.ToModel()).ToList();
    }

    public async Task<IReadOnlyList<BootstrapRequest>> GetQueuedBeforeAsync(DateTime cutoffUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.QueryAsync<BootstrapRequestRow>(
            new CommandDefinition($@"
                SELECT {RequestSelectColumns} FROM {TableName}
                WHERE status = 'queued' AND updated_at <= @Cutoff",
                new { Cutoff = cutoffUtc }, cancellationToken: ct));
        return rows.Select(r => r.ToModel()).ToList();
    }

    public async Task<bool> TryMarkRecoveryFailedAsync(
        BootstrapRecoveryExpectation expectation, string errorCode, string errorMessage, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var changed = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {TableName}
            SET status = 'failed', finished_at = NOW(),
                error_code = @ErrorCode, error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), updated_at = NOW()
            WHERE request_id = @RequestId
              AND status = @ExpectedStatus
              AND COALESCE(hangfire_job_id, '') = @ExpectedHangfireJobId
              AND reconcile_attempt_count = @ExpectedReconcileAttemptCount",
            new { expectation.RequestId, expectation.ExpectedStatus, expectation.ExpectedHangfireJobId,
                expectation.ExpectedReconcileAttemptCount, ErrorCode = errorCode, ErrorMessage = errorMessage,
                MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength },
            cancellationToken: ct));
        return changed == 1;
    }

    public async Task<bool> TryClaimScalableRecoveryAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string claimToken,
        bool isRecovery,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH request_update AS (
                UPDATE {TableName}
                SET reconcile_claim_token = @ClaimToken,
                    reconcile_claimed_at = NOW(),
                    updated_at = NOW()
                WHERE request_id = @RequestId
                  AND status = @ExpectedStatus
                  AND COALESCE(hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND (
                      reconcile_claim_token IS NULL
                      OR reconcile_claimed_at <= @StaleClaimBeforeUtc
                  )
                  AND (
                      @IsRecovery = FALSE
                      OR reconcile_attempt_count < @MaxRecoveryAttempts
                  )
                RETURNING request_id
            )
            UPDATE sync_meta.bootstrap_parent
            SET phase_claim_token = @ClaimToken,
                phase_claimed_at = NOW(),
                last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status = @ExpectedParentStatus
              AND COALESCE(phase_job_id, '') = COALESCE(@ExpectedPhaseJobId, '')
              AND (
                  phase_claim_token IS NULL
                  OR phase_claimed_at <= @StaleClaimBeforeUtc
              )
              AND EXISTS (SELECT 1 FROM request_update)
            RETURNING 1
            """, new
        {
            requestExpectation.RequestId,
            requestExpectation.ExpectedStatus,
            requestExpectation.ExpectedHangfireJobId,
            requestExpectation.ExpectedReconcileAttemptCount,
            parentExpectation.ParentId,
            parentExpectation.FencingToken,
            ExpectedParentStatus = parentExpectation.ExpectedStatus,
            ExpectedPhaseJobId = parentExpectation.ExpectedPhaseJobId,
            ClaimToken = claimToken,
            StaleClaimBeforeUtc = parentExpectation.StaleClaimBeforeUtc,
            IsRecovery = isRecovery,
            MaxRecoveryAttempts = BootstrapRecoverySchedulePolicy.MaxSuccessfulRecoveries
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFinalizeScalableRecoveryClaimAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string claimToken,
        string finalJobId,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET phase_job_id = @FinalJobId,
                    phase_job_kind = @ExpectedParentStatus,
                    phase_claim_token = NULL,
                    phase_claimed_at = NULL,
                    last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND COALESCE(parent.phase_job_id, '') = COALESCE(@ExpectedPhaseJobId, '')
                  AND parent.phase_claim_token = @ClaimToken
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'queued',
                    hangfire_job_id = @FinalJobId,
                    reconcile_claim_token = NULL,
                    reconcile_claimed_at = NULL,
                    reconcile_attempt_count = reconcile_attempt_count + 1,
                    first_recovery_at = COALESCE(first_recovery_at, NOW()),
                    last_recovery_at = NOW(),
                    error_code = NULL,
                    error_message = NULL,
                    updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND request.reconcile_claim_token = @ClaimToken
                  AND request.reconcile_attempt_count < @MaxRecoveryAttempts
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            requestExpectation.RequestId,
            requestExpectation.ExpectedStatus,
            requestExpectation.ExpectedHangfireJobId,
            requestExpectation.ExpectedReconcileAttemptCount,
            parentExpectation.ParentId,
            parentExpectation.FencingToken,
            ExpectedParentStatus = parentExpectation.ExpectedStatus,
            ExpectedPhaseJobId = parentExpectation.ExpectedPhaseJobId,
            ClaimToken = claimToken,
            FinalJobId = finalJobId,
            MaxRecoveryAttempts = BootstrapRecoverySchedulePolicy.MaxSuccessfulRecoveries
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryRecordScalablePhaseSchedulingFailureAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string code, string message, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET phase_schedule_failure_count = parent.phase_schedule_failure_count + 1,
                    phase_next_reconcile_at = NOW() + CASE parent.phase_schedule_failure_count
                        WHEN 0 THEN INTERVAL '1 minute'
                        WHEN 1 THEN INTERVAL '5 minutes'
                        ELSE INTERVAL '15 minutes' END,
                    phase_claim_token = CASE WHEN parent.phase_schedule_failure_count + 1 >= @MaxFailures
                                             THEN NULL ELSE parent.phase_claim_token END,
                    phase_claimed_at = CASE WHEN parent.phase_schedule_failure_count + 1 >= @MaxFailures
                                            THEN NULL ELSE parent.phase_claimed_at END,
                    status = CASE WHEN parent.phase_schedule_failure_count + 1 >= @MaxFailures
                                  THEN 'failed' ELSE parent.status END,
                    error_code = @Code,
                    error_message = LEFT(@Message, @MaxLength),
                    last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                  AND parent.phase_claim_token = @ClaimToken
                  AND parent.phase_schedule_failure_count < @MaxFailures
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET schedule_failure_count = request.schedule_failure_count + 1,
                    next_reconcile_at = NOW() + CASE request.schedule_failure_count
                        WHEN 0 THEN INTERVAL '1 minute'
                        WHEN 1 THEN INTERVAL '5 minutes'
                        ELSE INTERVAL '15 minutes' END,
                    status = CASE WHEN request.schedule_failure_count + 1 >= @MaxFailures
                                  THEN 'failed' ELSE request.status END,
                    finished_at = CASE WHEN request.schedule_failure_count + 1 >= @MaxFailures
                                       THEN NOW() ELSE request.finished_at END,
                    reconcile_claim_token = CASE WHEN request.schedule_failure_count + 1 >= @MaxFailures
                                                 THEN NULL ELSE request.reconcile_claim_token END,
                    reconcile_claimed_at = CASE WHEN request.schedule_failure_count + 1 >= @MaxFailures
                                                THEN NULL ELSE request.reconcile_claimed_at END,
                    error_code = @Code,
                    error_message = LEFT(@Message, @MaxLength),
                    updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND request.reconcile_claim_token = @ClaimToken
                  AND request.schedule_failure_count < @MaxFailures
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            requestExpectation.RequestId,
            requestExpectation.ExpectedStatus,
            requestExpectation.ExpectedHangfireJobId,
            requestExpectation.ExpectedReconcileAttemptCount,
            ParentId = parentExpectation.ParentId,
            FencingToken = parentExpectation.FencingToken,
            ExpectedParentStatus = parentExpectation.ExpectedStatus,
            ExpectedPhaseJobId = (object?)parentExpectation.ExpectedPhaseJobId ?? DBNull.Value,
            ClaimToken = parentExpectation.ClaimToken,
            Code = code,
            Message = message,
            MaxLength = BootstrapRecoveryConstants.MaxPersistedErrorLength,
            MaxFailures = BootstrapRecoverySchedulePolicy.MaxConsecutiveScheduleFailures
        }, transaction, cancellationToken: ct));

        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFailScalableChildRecoveryAsync(
        BootstrapRecoveryExpectation expectation,
        BootstrapChildFailureExpectation childExpectation,
        Guid parentId,
        Guid fencingToken,
        string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET status = 'failed',
                    phase_claim_token = NULL,
                    phase_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                    last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.last_heartbeat_at IS NOT DISTINCT FROM @ExpectedLastHeartbeatAt
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                  AND EXISTS (
                      SELECT 1 FROM sync_meta.bootstrap_child child
                      WHERE child.child_id = @ChildId
                        AND child.parent_id = @ChildParentId
                        AND child.status = @ExpectedChildStatus
                        AND COALESCE(child.hangfire_job_id, '') = COALESCE(@ExpectedChildJobId, '')
                  )
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'failed',
                    finished_at = NOW(),
                    reconcile_claim_token = NULL,
                    reconcile_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                    updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            expectation.RequestId,
            expectation.ExpectedStatus,
            expectation.ExpectedHangfireJobId,
            expectation.ExpectedReconcileAttemptCount,
            ParentId = parentId,
            FencingToken = fencingToken,
            ExpectedParentStatus = expectedParentStatus,
            ExpectedLastHeartbeatAt = (object?)expectedLastHeartbeatAt ?? DBNull.Value,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ChildId = childExpectation.ChildId,
            ChildParentId = childExpectation.ParentId,
            ExpectedChildStatus = childExpectation.ExpectedStatus,
            ExpectedChildJobId = childExpectation.ExpectedJobId,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFailScalableChildAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapChildFailureExpectation childExpectation,
        Guid fencingToken,
        string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH child_update AS (
                UPDATE sync_meta.bootstrap_child AS child
                SET status = 'failed', error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                    reconcile_claim_token = NULL, reconcile_claimed_at = NULL
                WHERE child.child_id = @ChildId
                  AND child.parent_id = @ParentId
                  AND child.status = @ExpectedChildStatus
                  AND COALESCE(child.hangfire_job_id, '') = COALESCE(@ExpectedChildJobId, '')
                RETURNING child.child_id
            ), parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET status = 'failed', phase_claim_token = NULL, phase_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.last_heartbeat_at IS NOT DISTINCT FROM @ExpectedLastHeartbeatAt
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                  AND EXISTS (SELECT 1 FROM child_update)
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'failed', finished_at = NOW(),
                    reconcile_claim_token = NULL, reconcile_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            requestExpectation.RequestId,
            requestExpectation.ExpectedStatus,
            requestExpectation.ExpectedHangfireJobId,
            requestExpectation.ExpectedReconcileAttemptCount,
            childExpectation.ChildId,
            childExpectation.ParentId,
            ExpectedChildStatus = childExpectation.ExpectedStatus,
            ExpectedChildJobId = childExpectation.ExpectedJobId,
            FencingToken = fencingToken,
            ExpectedParentStatus = expectedParentStatus,
            ExpectedLastHeartbeatAt = (object?)expectedLastHeartbeatAt ?? DBNull.Value,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFailScalableChildSchedulingExhaustedAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapChildFailureExpectation childExpectation,
        Guid fencingToken, string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt, string? expectedPhaseJobId,
        string errorCode, string errorMessage, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH child_update AS (
                UPDATE sync_meta.bootstrap_child AS child
                SET status = 'failed', reconcile_claim_token = NULL, reconcile_claimed_at = NULL,
                    error_code = @ErrorCode, error_message = LEFT(@ErrorMessage, @MaxLength)
                WHERE child.child_id = @ChildId AND child.parent_id = @ChildParentId
                  AND child.status = @ExpectedChildStatus
                  AND COALESCE(child.hangfire_job_id, '') = COALESCE(@ExpectedChildJobId, '')
                  AND child.schedule_failure_count >= @MaxFailures
                RETURNING child.child_id
            ), parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET status = 'failed', phase_claim_token = NULL, phase_claimed_at = NULL,
                    error_code = @ErrorCode, error_message = LEFT(@ErrorMessage, @MaxLength),
                    last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.last_heartbeat_at IS NOT DISTINCT FROM @ExpectedLastHeartbeatAt
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                  AND EXISTS (SELECT 1 FROM child_update)
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'failed', finished_at = NOW(), reconcile_claim_token = NULL,
                    reconcile_claimed_at = NULL, error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxLength), updated_at = NOW()
                WHERE request.request_id = @RequestId AND request.status = @ExpectedRequestStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            ) SELECT COUNT(*) FROM request_update
            """, new
        {
            requestExpectation.RequestId,
            ExpectedRequestStatus = requestExpectation.ExpectedStatus,
            ExpectedHangfireJobId = requestExpectation.ExpectedHangfireJobId,
            ExpectedReconcileAttemptCount = requestExpectation.ExpectedReconcileAttemptCount,
            ChildId = childExpectation.ChildId, ChildParentId = childExpectation.ParentId,
            ExpectedChildStatus = childExpectation.ExpectedStatus,
            ExpectedChildJobId = childExpectation.ExpectedJobId,
            ParentId = childExpectation.ParentId, FencingToken = fencingToken,
            ExpectedParentStatus = expectedParentStatus,
            ExpectedLastHeartbeatAt = (object?)expectedLastHeartbeatAt ?? DBNull.Value,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ErrorCode = "BootstrapRecoverySchedulingExhausted", ErrorMessage = errorMessage,
            MaxLength = BootstrapRecoveryConstants.MaxPersistedErrorLength,
            MaxFailures = BootstrapRecoverySchedulePolicy.MaxConsecutiveScheduleFailures
        }, tx, cancellationToken: ct));
        if (count != 1)
        {
            await tx.RollbackAsync(ct);
            return false;
        }
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFailScalableAsync(
        BootstrapRecoveryExpectation requestExpectation,
        Guid parentId,
        Guid fencingToken,
        string expectedParentStatus,
        DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        string errorCode,
        string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET status = 'failed', phase_claim_token = NULL, phase_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.last_heartbeat_at IS NOT DISTINCT FROM @ExpectedLastHeartbeatAt
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'failed', finished_at = NOW(),
                    reconcile_claim_token = NULL, reconcile_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            requestExpectation.RequestId,
            requestExpectation.ExpectedStatus,
            requestExpectation.ExpectedHangfireJobId,
            requestExpectation.ExpectedReconcileAttemptCount,
            ParentId = parentId,
            FencingToken = fencingToken,
            ExpectedParentStatus = expectedParentStatus,
            ExpectedLastHeartbeatAt = (object?)expectedLastHeartbeatAt ?? DBNull.Value,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFailScalableRecoveryClaimAsync(
        BootstrapRecoveryExpectation requestExpectation,
        BootstrapParentPhaseJobExpectation parentExpectation,
        string claimToken,
        string errorCode,
        string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET status = 'failed', phase_claim_token = NULL, phase_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND COALESCE(parent.phase_job_id, '') = COALESCE(@ExpectedPhaseJobId, '')
                  AND parent.phase_claim_token = @ClaimToken
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'failed', finished_at = NOW(),
                    reconcile_claim_token = NULL, reconcile_claimed_at = NULL,
                    error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength), updated_at = NOW()
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND request.reconcile_claim_token = @ClaimToken
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            requestExpectation.RequestId,
            requestExpectation.ExpectedStatus,
            requestExpectation.ExpectedHangfireJobId,
            requestExpectation.ExpectedReconcileAttemptCount,
            parentExpectation.ParentId,
            parentExpectation.FencingToken,
            ExpectedParentStatus = parentExpectation.ExpectedStatus,
            ExpectedPhaseJobId = parentExpectation.ExpectedPhaseJobId,
            ClaimToken = claimToken,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryFailInconsistentPhaseStateAsync(
        BootstrapRecoveryExpectation expectation,
        Guid parentId, Guid fencingToken,
        string expectedParentStatus, DateTime? expectedLastHeartbeatAt,
        string? expectedPhaseJobId,
        DateTime claimExpiredAt,
        string errorCode, string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition($"""
            WITH parent_update AS (
                UPDATE sync_meta.bootstrap_parent AS parent
                SET status = 'failed', error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                    last_heartbeat_at = NOW()
                WHERE parent.parent_id = @ParentId
                  AND parent.bootstrap_request_id = @RequestId
                  AND parent.fencing_token = @FencingToken
                  AND parent.status = @ExpectedParentStatus
                  AND parent.last_heartbeat_at IS NOT DISTINCT FROM @ExpectedLastHeartbeatAt
                  AND parent.phase_job_id IS NOT DISTINCT FROM @ExpectedPhaseJobId
                  AND (parent.phase_claim_token IS NULL OR parent.phase_claimed_at <= @ClaimExpiredAt)
                RETURNING parent.parent_id
            ), request_update AS (
                UPDATE {TableName} AS request
                SET status = 'failed', finished_at = NOW(), error_code = @ErrorCode,
                    error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                    updated_at = NOW(),
                    reconcile_claim_token = NULL, reconcile_claimed_at = NULL
                WHERE request.request_id = @RequestId
                  AND request.status = @ExpectedRequestStatus
                  AND COALESCE(request.hangfire_job_id, '') = @ExpectedHangfireJobId
                  AND request.reconcile_attempt_count = @ExpectedReconcileAttemptCount
                  AND EXISTS (SELECT 1 FROM parent_update)
                RETURNING request.request_id
            )
            SELECT COUNT(*) FROM request_update
            """, new
        {
            RequestId = expectation.RequestId,
            ExpectedRequestStatus = expectation.ExpectedStatus,
            ExpectedHangfireJobId = expectation.ExpectedHangfireJobId,
            ExpectedReconcileAttemptCount = expectation.ExpectedReconcileAttemptCount,
            ParentId = parentId, FencingToken = fencingToken,
            ExpectedParentStatus = expectedParentStatus,
            ExpectedLastHeartbeatAt = (object?)expectedLastHeartbeatAt ?? DBNull.Value,
            ExpectedPhaseJobId = (object?)expectedPhaseJobId ?? DBNull.Value,
            ClaimExpiredAt = claimExpiredAt,
            ErrorCode = errorCode, ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength
        }, transaction, cancellationToken: ct));
        if (count != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<BootstrapRequest>> GetStaleActiveBeforeAsync(
        string status, DateTime cutoffUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.QueryAsync<BootstrapRequestRow>(
            new CommandDefinition($@"
                SELECT {RequestSelectColumns} FROM {TableName}
                WHERE status = @Status AND updated_at <= @Cutoff",
                new { Status = status, Cutoff = cutoffUtc }, cancellationToken: ct));
        return rows.Select(r => r.ToModel()).ToList();
    }

    private sealed record BootstrapRequestRow
    {
        public Guid RequestId { get; init; }
        public required string SourceTable { get; init; }
        public required string Status { get; init; }
        public string BootstrapType { get; init; } = BootstrapRequestType.InMemory;
        public string? HangfireJobId { get; init; }
        public long RowsStaged { get; init; }
        public long? TotalRowsExpected { get; init; }
        public int AttemptCount { get; init; }
        public int ReconcileAttemptCount { get; init; }
        public int ScheduleFailureCount { get; init; }
        public DateTime? NextReconcileAt { get; init; }
        public DateTime RequestedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? StartedAt { get; init; }
        public DateTime? FinishedAt { get; init; }
        public DateTime? FirstRecoveryAt { get; init; }
        public DateTime? LastRecoveryAt { get; init; }
        public string? ReconcileClaimToken { get; init; }
        public DateTime? ReconcileClaimedAt { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public BootstrapRequest ToModel() => new()
        {
            RequestId = RequestId, SourceTable = SourceTable, Status = Status, BootstrapType = BootstrapType,
            HangfireJobId = HangfireJobId, RowsStaged = RowsStaged, TotalRowsExpected = TotalRowsExpected,
            AttemptCount = AttemptCount, ReconcileAttemptCount = ReconcileAttemptCount,
            ScheduleFailureCount = ScheduleFailureCount, NextReconcileAt = NextReconcileAt,
            RequestedAt = RequestedAt, UpdatedAt = UpdatedAt, StartedAt = StartedAt, FinishedAt = FinishedAt,
            FirstRecoveryAt = FirstRecoveryAt, LastRecoveryAt = LastRecoveryAt,
            ReconcileClaimToken = ReconcileClaimToken, ReconcileClaimedAt = ReconcileClaimedAt,
            ErrorCode = ErrorCode, ErrorMessage = ErrorMessage
        };
    }
}
