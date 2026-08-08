using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Microsoft.Extensions.Logging;

namespace Application.Features.CentralDbSync.Services;

/// <summary>
/// Executes a single child bootstrap job within the scalable parent-child flow.
/// Each child reads at most 10,000 rows via keyset cursor, stages them via
/// binary COPY into the dynamic staging table, and enqueues the next child.
/// Transient failures (PostgreSQL/network/timeout/deadlock) retry up to 3 times.
/// </summary>
public sealed class BootstrapChildService(
    IStagedBootstrapSourceReader sourceReader,
    ITypedBootstrapStagingStore stagingStore,
    IBootstrapParentStore parentStore,
    IBootstrapChildStore childStore,
    IMappingRuleProvider ruleProvider,
    IBootstrapJobScheduler jobScheduler,
    IBootstrapDiagnosticEventStore eventStore,
    BootstrapFailureService failureService,
    ILogger<BootstrapChildService> logger)
{
    private readonly IBootstrapDiagnosticEventStore _eventStore = eventStore;
    public const int DefaultBatchSize = 10_000;
    public const int MaxRetries = 3;

    /// <summary>
    /// Runs a single child bootstrap job: reads a batch from SQL Server,
    /// stages it via COPY+upsert into the dynamic staging table,
    /// persists cursor progress, and enqueues the next child if EOF not reached.
    /// </summary>
    public async Task<BootstrapChildResult> RunAsync(
        Guid childId,
        Guid parentId,
        Guid fencingToken,
        string stagingSchema,
        string stagingTableName,
        CancellationToken ct)
    {
        var child = await childStore.GetAsync(childId, ct);
        if (child is null)
        {
            return BootstrapChildResult.Fail("ChildNotFound",
                $"Child {childId} not found for parent {parentId}.");
        }

        var parent = await parentStore.GetAsync(parentId, ct);
        if (parent is null)
        {
            return BootstrapChildResult.Fail("ParentNotFound",
                $"Parent {parentId} not found for child {childId}.");
        }

        // Claim child: Queued → Running (CAS guard)
        var claimed = await childStore.TryClaimAsync(childId, parentId, fencingToken, ct);
        if (!claimed)
        {
            return BootstrapChildResult.Fail("ClaimFailed",
                $"Child {childId} could not be claimed (already running or completed).");
        }

        var rule = ruleProvider.Get(parent.RuleName);

        async Task<bool> OwnsParentAsync()
        {
            var current = await parentStore.GetAsync(parentId, ct);
            if (current is null)
                return false;
            if (current.Status == BootstrapParentStatus.CancelRequested)
            {
                await _eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                    parent.BootstrapRequestId ?? Guid.Empty, parentId, childId,
                    BootstrapDiagnosticEntityType.Child, BootstrapDiagnosticEventType.CancellationObserved,
                    BootstrapParentStatus.Running, BootstrapParentStatus.CancelRequested,
                    null, null, child.Sequence, null,
                    "CancellationObserved", "Child worker observed cancellation", "system"), ct);
                return false;
            }
            return current.Status == BootstrapParentStatus.Running
                && current.FencingToken == fencingToken;
        }

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (!await OwnsParentAsync())
                {
                    BootstrapLifecycleMetrics.StaleWorkerStopped.Add(1,
                        new KeyValuePair<string, object?>("entity", "child"));
                    return BootstrapChildResult.Fail("StaleParentOwnership", "Parent ownership was lost before reading.");
                }

                // Read batch
                var afterKey = child.AfterKey;
                var rows = await sourceReader.ReadBatchAsync(
                    rule, afterKey, DefaultBatchSize, ct);

                // Stage batch
                await stagingStore.StageBatchAsync(
                    rule, stagingSchema, stagingTableName, rows, ct);

                if (!await OwnsParentAsync())
                    return BootstrapChildResult.Fail("StaleParentOwnership", "Parent ownership was lost after staging.");

                // Rows are keyed by the source column aliases produced by the batch reader.
                var keyColumn = rule.Source.PrimaryKey[0];
                var lastKey = rows.Count > 0
                    ? rows[^1].GetValueOrDefault(keyColumn)?.ToString()
                    : child.AfterKey;

                var isEof = rows.Count < DefaultBatchSize;

                if (!isEof && string.IsNullOrEmpty(lastKey))
                {
                    // Without a cursor the next child would re-read the same batch forever.
                    const string code = "MissingKeysetCursor";
                    var message =
                        $"Batch for rule {rule.RuleName} yielded no value for key column " +
                        $"'{keyColumn}'; cannot advance the keyset cursor.";

                    logger.LogError("Child {ChildId} for parent {ParentId}: {Message}",
                        childId, parentId, message);

                    await failureService.FailAsync(parent,
                        new BootstrapChildFailureExpectation(childId, parentId,
                            BootstrapChildStatus.Running, child.HangfireJobId),
                        code, message, ct);
                    return BootstrapChildResult.Fail(code, message);
                }

                if (!await OwnsParentAsync())
                    return BootstrapChildResult.Fail("StaleParentOwnership", "Parent ownership was lost before completion.");

                var completed = await childStore.TryCompleteAsync(
                    childId, parentId, fencingToken, lastKey, rows.Count, ct);

                if (!completed)
                {
                    logger.LogWarning(
                        "Child {ChildId} for parent {ParentId} could not be marked completed",
                        childId, parentId);
                    return BootstrapChildResult.Fail("ConcurrentCompletion",
                        "Child was already completed by another worker.");
                }

                // Update parent progress
                if (!await parentStore.UpdateProgressAsync(
                        parentId, fencingToken,
                        lastKey, parent.RowsStaged + rows.Count, null, ct))
                    return BootstrapChildResult.Fail("StaleParentOwnership", "Parent ownership was lost before progress update.");

                // Enqueue next child if there are more rows
                if (!isEof)
                {
                    if (!await OwnsParentAsync())
                        return BootstrapChildResult.Fail("StaleParentOwnership", "Parent ownership was lost before next child.");
                    var nextChild = await childStore.CreateNextAsync(
                        parentId, lastKey, ct);
                    var nextClaimToken = Guid.NewGuid().ToString("N");
                    if (!await childStore.TryClaimInitialAsync(nextChild.ChildId, parentId,
                            fencingToken, nextClaimToken, ct))
                        return BootstrapChildResult.Fail("StaleParentOwnership", "Next child claim was lost.");
                    await jobScheduler.ScheduleClaimedChildAsync(parent.RuleName, parentId,
                        nextChild.ChildId, fencingToken, BootstrapChildStatus.PendingEnqueue,
                        nextClaimToken, ct);
                }
                else
                {
                    if (!await OwnsParentAsync())
                        return BootstrapChildResult.Fail("StaleParentOwnership", "Parent ownership was lost before finalize scheduling.");
                    // A phase claim is acquired before scheduling. The Hangfire job records
                    // its actual id before coordinator effects, so any surplus job has no authority.
                    var token = Guid.NewGuid().ToString("N");
                    if (await parentStore.TryClaimPhaseJobAsync(parentId, fencingToken,
                            BootstrapParentStatus.Running, null, token, DateTime.UtcNow, ct))
                    {
                        await jobScheduler.ScheduleClaimedFinalizeAsync(parent.RuleName, parentId,
                            fencingToken, BootstrapParentStatus.Running, token, ct);
                    }
                }

                logger.LogInformation(
                    "Child {ChildId} (seq {Sequence}) for parent {ParentId}: " +
                    "{RowCount} rows, lastKey={LastKey}, isEof={IsEof}",
                    childId, child.Sequence, parentId, rows.Count, lastKey, isEof);

                return BootstrapChildResult.Success(rows.Count, isEof, lastKey);
            }
            catch (Exception ex) when (attempt < MaxRetries && IsTransient(ex))
            {
                logger.LogWarning(ex,
                    "Transient error in child {ChildId} (attempt {Attempt}/{MaxRetries})",
                    childId, attempt, MaxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Fatal error in child {ChildId} (attempt {Attempt}/{MaxRetries})",
                    childId, attempt, MaxRetries);

                const string code = "ChildFailed";
                await failureService.FailAsync(parent,
                    new BootstrapChildFailureExpectation(childId, parentId,
                        BootstrapChildStatus.Running, child.HangfireJobId),
                    code, BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Child bootstrap failed.", ct);

                return BootstrapChildResult.Fail(code,
                    BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "Child bootstrap failed.");
            }
        }

        // Unreachable: the transient filter stops applying on the final attempt, so the
        // last failure always exits through the fatal catch above.
        return BootstrapChildResult.Fail("RetriesExhausted",
            $"Child {childId} failed after {MaxRetries} attempts.");
    }

    private static bool IsTransient(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || message.Contains("network", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Result of a single child execution.
/// </summary>
public sealed record BootstrapChildResult
{
    public bool IsSuccess { get; init; }
    public int RowsRead { get; init; }
    public bool IsEof { get; init; }
    public string? LastKey { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static BootstrapChildResult Success(int rowsRead, bool isEof, string? lastKey) => new()
    {
        IsSuccess = true,
        RowsRead = rowsRead,
        IsEof = isEof,
        LastKey = lastKey
    };

    public static BootstrapChildResult Fail(string code, string message) => new()
    {
        IsSuccess = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
