namespace Infrastructure.CentralDbSync;

using Application.Common.Exceptions;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Dapper;
using Npgsql;

/// <summary>
/// PostgreSQL implementation of <see cref="IBootstrapParentStore"/>.
/// Every update includes parent ID, fencing token, and expected status in its WHERE clause
/// so a zero-row update means the caller is stale and must stop immediately.
/// </summary>
public sealed class PostgresBootstrapParentStore(string connectionString) : IBootstrapParentStore
{
    private const string SelectColumns = """
        parent_id AS "ParentId",
        rule_name AS "RuleName",
        source_table AS "SourceTable",
        target_schema AS "TargetSchema",
        target_table AS "TargetTable",
        status AS "Status",
        fencing_token AS "FencingToken",
        baseline_version AS "BaselineVersion",
        watermark_version AS "WatermarkVersion",
        last_processed_key AS "LastProcessedKey",
        rows_staged AS "RowsStaged",
        total_rows_expected AS "TotalRowsExpected",
        attempt_count AS "AttemptCount",
        created_at AS "CreatedAt",
        last_heartbeat_at AS "LastHeartbeatAt",
        completed_at AS "CompletedAt",
        staging_schema AS "StagingSchema",
        staging_table_name AS "StagingTableName",
        staging_created_at AS "StagingCreatedAt",
        cleanup_completed_at AS "CleanupCompletedAt",
        deferred_ct_pending AS "DeferredCtPending",
        bootstrap_request_id AS "BootstrapRequestId",
        phase_job_id AS "PhaseJobId",
        phase_job_kind AS "PhaseJobKind",
        phase_claim_token AS "PhaseClaimToken",
        phase_claimed_at AS "PhaseClaimedAt",
        phase_schedule_failure_count AS "PhaseScheduleFailureCount",
        phase_next_reconcile_at AS "PhaseNextReconcileAt",
        error_code AS "ErrorCode",
        error_message AS "ErrorMessage",
        cancel_requested_at AS "CancelRequestedAt",
        cancel_requested_by AS "CancelRequestedBy"
        """;

    public async Task<BootstrapParent> CreateAsync(
        string ruleName, string sourceTable, string targetSchema,
        string targetTable, string stagingTableName,
        Guid? bootstrapRequestId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var parentId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var fencingToken = Guid.NewGuid();

        var sql = $"""
            INSERT INTO sync_meta.bootstrap_parent
                (parent_id, created_at, rule_name, source_table,
                 target_schema, target_table, status, fencing_token,
                 rows_staged, attempt_count, staging_schema, staging_table_name,
                 deferred_ct_pending, bootstrap_request_id)
            VALUES
                (@ParentId, @CreatedAt, @RuleName, @SourceTable,
                 @TargetSchema, @TargetTable, @Status, @FencingToken,
                 @RowsStaged, @AttemptCount, @StagingSchema, @StagingTableName,
                 @DeferredCtPending, @BootstrapRequestId)
            RETURNING
                {SelectColumns}
            """;

        try
        {
            return await conn.QuerySingleAsync<BootstrapParent>(sql, new
            {
                ParentId = parentId,
                CreatedAt = now,
                RuleName = ruleName,
                SourceTable = sourceTable,
                TargetSchema = targetSchema,
                TargetTable = targetTable,
                Status = BootstrapParentStatus.PendingEnqueue,
                FencingToken = fencingToken,
                RowsStaged = 0,
                AttemptCount = 0,
                StagingSchema = "sync_meta",
                StagingTableName = stagingTableName,
                DeferredCtPending = false,
                BootstrapRequestId = bootstrapRequestId
            });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new BootstrapConflictException(
                $"Rule '{ruleName}' already has an active scalable bootstrap parent.");
        }
    }

    public async Task<BootstrapParent?> GetAsync(Guid parentId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<BootstrapParent>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_parent
            WHERE parent_id = @ParentId
            """,
            new { ParentId = parentId });
    }

    public async Task<BootstrapParent?> GetByRuleNameAsync(string ruleName, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<BootstrapParent>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_parent
            WHERE rule_name = @RuleName
            ORDER BY created_at DESC
            LIMIT 1
            """,
            new { RuleName = ruleName });
    }

    public async Task<bool> TryClaimAsync(Guid parentId, Guid fencingToken, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET status = @NewStatus, last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status = @FromStatus
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            FromStatus = BootstrapParentStatus.PendingEnqueue,
            NewStatus = BootstrapParentStatus.Running
        });

        return rows > 0;
    }

    public async Task<bool> TryTransitionAsync(Guid parentId, Guid fencingToken,
        string fromStatus, string toStatus, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET status = @ToStatus, last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status = @FromStatus
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            FromStatus = fromStatus,
            ToStatus = toStatus
        });

        return rows > 0;
    }

    public async Task<bool> HeartbeatAsync(Guid parentId, Guid fencingToken, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId AND fencing_token = @FencingToken
              AND status = 'running'
            """, new { ParentId = parentId, FencingToken = fencingToken });

        return rows > 0;
    }

    public async Task<bool> SetBaselineVersionAsync(Guid parentId, Guid fencingToken,
        long baselineVersion, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET baseline_version = @BaselineVersion,
                last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId AND fencing_token = @FencingToken
              AND status = 'running'
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            BaselineVersion = baselineVersion
        });

        return rows > 0;
    }

    public async Task<bool> SetStagingCreatedAsync(Guid parentId, Guid fencingToken, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET staging_created_at = NOW(), last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId AND fencing_token = @FencingToken
              AND status = 'running'
            """, new { ParentId = parentId, FencingToken = fencingToken });

        return rows > 0;
    }

    public async Task<bool> UpdateProgressAsync(Guid parentId, Guid fencingToken,
        string? lastProcessedKey, long rowsStaged, long? totalRowsExpected, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET last_processed_key = @LastProcessedKey,
                rows_staged = GREATEST(COALESCE(rows_staged, 0), @RowsStaged),
                total_rows_expected = COALESCE(@TotalRowsExpected, total_rows_expected),
                last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId AND fencing_token = @FencingToken
              AND status = 'running'
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            LastProcessedKey = lastProcessedKey,
            RowsStaged = rowsStaged,
            TotalRowsExpected = totalRowsExpected
        });

        return rows > 0;
    }

    public async Task<bool> MarkCtCatchUpAsync(Guid parentId, Guid fencingToken,
        long watermark, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET status = @NewStatus,
                watermark_version = @Watermark,
                last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status = @FromStatus
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            FromStatus = BootstrapParentStatus.Running,
            NewStatus = BootstrapParentStatus.CatchingUp,
            Watermark = watermark
        });

        return rows > 0;
    }

    public async Task<IReadOnlyList<BootstrapParent>> GetStaleParentsAsync(
        DateTime cutoffUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var results = await conn.QueryAsync<BootstrapParent>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_parent
            WHERE status IN (@PendingEnqueue, @Running, @CatchingUp, @Publishing, @CancelRequested)
              AND last_heartbeat_at < @Cutoff
            """, new
        {
            PendingEnqueue = BootstrapParentStatus.PendingEnqueue,
            Running = BootstrapParentStatus.Running,
            CatchingUp = BootstrapParentStatus.CatchingUp,
            Publishing = BootstrapParentStatus.Publishing,
            CancelRequested = BootstrapParentStatus.CancelRequested,
            Cutoff = cutoffUtc
        });

        return results.AsList();
    }

    public async Task<IReadOnlyList<BootstrapParent>> GetCleanupCandidatesAsync(
        DateTime cutoffUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var results = await conn.QueryAsync<BootstrapParent>(
            $"""
            SELECT {SelectColumns}
            FROM sync_meta.bootstrap_parent
            WHERE (
                (status IN (@Failed, @RecoveryPending, @CancelRequested, @Cancelled)
                 AND last_heartbeat_at < @Cutoff
                 AND staging_created_at IS NOT NULL)
                OR
                (status = @Expired AND cleanup_completed_at IS NULL)
            )
            """, new
        {
            Failed = BootstrapParentStatus.Failed,
            RecoveryPending = BootstrapParentStatus.RecoveryPending,
            CancelRequested = BootstrapParentStatus.CancelRequested,
            Cancelled = BootstrapParentStatus.Cancelled,
            Expired = BootstrapParentStatus.Expired,
            Cutoff = cutoffUtc
        });

        return results.AsList();
    }

    public async Task<bool> SetCleanupCompletedAsync(Guid parentId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET cleanup_completed_at = NOW()
            WHERE parent_id = @ParentId
            """, new { ParentId = parentId });

        return rows > 0;
    }

    public async Task<bool> TrySetDeferredCtAsync(Guid parentId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET deferred_ct_pending = TRUE
            WHERE parent_id = @ParentId
              AND deferred_ct_pending = FALSE
            """, new { ParentId = parentId });

        return rows > 0;
    }

    public async Task<bool> TryClaimPhaseJobAsync(Guid parentId, Guid fencingToken,
        string expectedStatus, string? expectedJobId, string claimToken,
        DateTime staleClaimBeforeUtc, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_parent
            SET phase_claim_token = @ClaimToken, phase_claimed_at = NOW(), last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId AND fencing_token = @FencingToken AND status = @ExpectedStatus
              AND COALESCE(phase_job_id, '') = COALESCE(@ExpectedJobId, '')
              AND (phase_next_reconcile_at IS NULL OR phase_next_reconcile_at <= NOW())
              AND (phase_claim_token IS NULL OR phase_claimed_at <= @StaleClaimBeforeUtc)
            """, new { ParentId = parentId, FencingToken = fencingToken, ExpectedStatus = expectedStatus,
                ExpectedJobId = expectedJobId, ClaimToken = claimToken, StaleClaimBeforeUtc = staleClaimBeforeUtc },
            cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryFinalizePhaseJobAsync(Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken, string actualJobId, string phaseKind, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_parent
            SET phase_job_id = @ActualJobId, phase_job_kind = @PhaseKind,
                phase_claim_token = NULL, phase_claimed_at = NULL, last_heartbeat_at = NOW()
                , phase_schedule_failure_count = 0, phase_next_reconcile_at = NULL
            WHERE parent_id = @ParentId AND fencing_token = @FencingToken AND status = @ExpectedStatus
              AND phase_claim_token = @ClaimToken
            """, new { ParentId = parentId, FencingToken = fencingToken, ExpectedStatus = expectedStatus,
                ClaimToken = claimToken, ActualJobId = actualJobId, PhaseKind = phaseKind }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryFailAsync(Guid parentId, Guid fencingToken,
        string errorCode, string errorMessage, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET status = @NewStatus,
                error_code = @ErrorCode,
                error_message = @ErrorMessage,
                last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status IN (@PendingEnqueue, @Running, @CatchingUp, @Publishing)
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            NewStatus = BootstrapParentStatus.Failed,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            PendingEnqueue = BootstrapParentStatus.PendingEnqueue,
            Running = BootstrapParentStatus.Running,
            CatchingUp = BootstrapParentStatus.CatchingUp,
            Publishing = BootstrapParentStatus.Publishing
        });

        return rows > 0;
    }

    public async Task<bool> TryFailPhaseClaimAsync(Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken,
        string errorCode, string errorMessage, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_parent
            SET phase_claim_token = NULL, phase_claimed_at = NULL,
                error_code = @ErrorCode, error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength)
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status = @ExpectedStatus
              AND phase_claim_token = @ClaimToken
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            ExpectedStatus = expectedStatus,
            ClaimToken = claimToken,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength
        }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryRecordPhaseClaimSchedulingFailureAsync(Guid parentId, Guid fencingToken,
        string expectedStatus, string claimToken, string errorCode, string errorMessage,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE sync_meta.bootstrap_parent
            SET error_code = @ErrorCode,
                error_message = LEFT(@ErrorMessage, @MaxPersistedErrorLength),
                phase_schedule_failure_count = phase_schedule_failure_count + 1,
                phase_next_reconcile_at = NOW() + CASE phase_schedule_failure_count
                    WHEN 0 THEN INTERVAL '1 minute' WHEN 1 THEN INTERVAL '5 minutes'
                    ELSE INTERVAL '15 minutes' END,
                phase_claim_token = CASE WHEN phase_schedule_failure_count + 1 >= @MaxFailures
                                         THEN NULL ELSE phase_claim_token END,
                phase_claimed_at = CASE WHEN phase_schedule_failure_count + 1 >= @MaxFailures
                                       THEN NULL ELSE phase_claimed_at END,
                status = CASE WHEN phase_schedule_failure_count + 1 >= @MaxFailures
                              THEN 'failed' ELSE status END
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status = @ExpectedStatus
              AND phase_claim_token = @ClaimToken
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            ExpectedStatus = expectedStatus,
            ClaimToken = claimToken,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            MaxPersistedErrorLength = BootstrapRecoveryConstants.MaxPersistedErrorLength,
            MaxFailures = BootstrapRecoverySchedulePolicy.MaxConsecutiveScheduleFailures
        }, cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> TryRequestCancelAsync(Guid parentId, Guid fencingToken,
        string initiatedBy, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET status = @NewStatus,
                cancel_requested_at = NOW(),
                cancel_requested_by = @InitiatedBy,
                last_heartbeat_at = NOW()
            WHERE parent_id = @ParentId
              AND fencing_token = @FencingToken
              AND status IN (@Running, @CatchingUp, @Publishing)
            """, new
        {
            ParentId = parentId,
            FencingToken = fencingToken,
            InitiatedBy = initiatedBy,
            NewStatus = BootstrapParentStatus.CancelRequested,
            Running = BootstrapParentStatus.Running,
            CatchingUp = BootstrapParentStatus.CatchingUp,
            Publishing = BootstrapParentStatus.Publishing
        });

        return rows > 0;
    }

    public async Task<bool> TryMarkCancelledAsync(Guid parentId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.ExecuteAsync("""
            UPDATE sync_meta.bootstrap_parent
            SET status = @NewStatus,
                cleanup_completed_at = NOW()
            WHERE parent_id = @ParentId
              AND status = @FromStatus
            """, new
        {
            ParentId = parentId,
            FromStatus = BootstrapParentStatus.CancelRequested,
            NewStatus = BootstrapParentStatus.Cancelled
        });

        return rows > 0;
    }
}
